namespace FCTracker;

using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using JetBrains.Annotations;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Windows;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.NativeWrapper;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using ECommons.Automation.NeoTaskManager;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;
using InteropGenerator.Runtime;
using Callback = ECommons.Automation.Callback;

[UsedImplicitly]
public sealed class FCTracker : IDalamudPlugin
{
    public static FCTracker Plugin { get; private set; } = null!;

    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    private const string CommandName = "/fctrack";

    public readonly WindowSystem windowSystem = new("FCTracker");
    private         MainWindow   MainWindow { get; init; } = null!;

    private readonly (string[], string, Action<string[]>)[] commands = null!;

    public TaskManager TaskManager { get; init; } = null!;

    public int Version { get; init; }

    public FCTracker()
    {
        try
        {
            Plugin = this;

            ECommonsMain.Init(PluginInterface, this, Module.DalamudReflector, Module.ObjectFunctions);



            /*
            EzConfig.DefaultSerializationFactory = new AutoDutySerializationFactory();
            LocalizationManager.Initialize();
            */

            EzConfig.DefaultSerializationFactory = new FCTrackerSerializationFactory();
            Configuration.Instance               = EzConfig.Init<Configuration>();

            DirectoryInfo configDirectory = PluginInterface.ConfigDirectory;

            this.Version =
                ((PluginInterface.IsDev ? new Version(0, 0, 0, 291) :
                  PluginInterface.IsTesting ? PluginInterface.Manifest.TestingAssemblyVersion ?? PluginInterface.Manifest.AssemblyVersion : PluginInterface.Manifest.AssemblyVersion)!).Revision;

            if (!configDirectory.Exists)
                configDirectory.Create();
            
            this.MainWindow = new MainWindow();
            this.windowSystem.AddWindow(this.MainWindow);


            this.TaskManager = new TaskManager(new TaskManagerConfiguration
                                               {
                                                   AbortOnTimeout  = false,
                                                   TimeoutSilently = true,
                                                   TimeLimitMS     = 10_000,
                                                   ShowDebug       = true
                                               });


            if (Svc.ClientState.IsLoggedIn)
                this.ClientStateOnLogin();

            this.commands =
            [
                (["config", "cfg"], "opens config window / modifies config", _ => this.ToggleConfigUi()),
            ];
            
            Svc.Commands.AddHandler("/fct", new CommandInfo(this.OnCommand));
            Svc.Commands.AddHandler(CommandName, new CommandInfo(this.OnCommand)
            {
                HelpMessage = string.Join("\n", this.commands.Select(tuple => $"/fctrack or /fct {string.Join(" / ", tuple.Item1)} -> {tuple.Item2}"))
            });

            Svc.ClientState.Login += this.ClientStateOnLogin;

            PluginInterface.UiBuilder.Draw         += this.windowSystem.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += this.ToggleConfigUi;
            PluginInterface.UiBuilder.OpenMainUi   += this.ToggleMainUi;
        }
        catch (Exception e)
        {
            Svc.Log.Info($"Failed loading plugin\n{e}");
        }
    }


    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw         -= this.windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= this.ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi   -= this.ToggleMainUi;
        Svc.ClientState.Login                  -= this.ClientStateOnLogin;

        this.windowSystem.RemoveAllWindows();

        //this.ConfigWindow.Dispose();
        this.MainWindow.Dispose();

        Svc.Commands.RemoveHandler(CommandName);
        Svc.Commands.RemoveHandler("/fct");
    }

    private void OnCommand(string command, string args)
    {
        Match match = RegexHelper.ArgumentParserRegex().Match(args.ToLower());
        List<string> matches = [];

        while (match.Success)
        {
            matches.Add(match.Groups[match.Groups[1].Length > 0 ? 1 : 0].Value);
            match = match.NextMatch();
        }

        string[] argsArray = matches.Count > 0 ? [.. matches] : [string.Empty];
        string check = argsArray[0];

        Svc.Log.Debug("command with: " + args);

        foreach ((string[] keywords, _, Action<string[]> action) in this.commands)
            if (keywords.Any(key => check.StartsWith(key)))
            {
                Svc.Log.Debug("Activating command: " + string.Join(" / ", keywords));
                action(argsArray);
                return;
            }

        switch (argsArray[0])
        {
            default:
                this.MainWindow.Toggle();
                break;
        }
    }

    private unsafe void ClientStateOnLogin()
    {
        this.GetFCInfo();
    }

    public unsafe void GetFCInfo()
    {
        this.TaskManager.Enqueue(() => PlayerHelper.IsReady);
        AgentFreeCompany* agentFreeCompany = AgentFreeCompany.Instance();
        this.TaskManager.Enqueue(() => agentFreeCompany->Show());
        this.TaskManager.Enqueue(() => agentFreeCompany->IsAddonShown());
        this.TaskManager.Enqueue(() => agentFreeCompany->IsAddonReady());
        this.TaskManager.Enqueue(() =>
                                 {
                                     if (GenericHelpers.TryGetAddonByName("FreeCompany", out AtkUnitBase* fcAddon) && fcAddon->IsReady())
                                     {
                                         Callback.Fire(fcAddon, true, 0, 5u);
                                         return true;
                                     }
                                     return false;
                                 });
        this.TaskManager.Enqueue(() => GenericHelpers.TryGetAddonByName("FreeCompanyStatus", out AtkUnitBase* fcAddon) && fcAddon->IsReady());
        this.TaskManager.Enqueue(() => PlayerHelper.IsReady);
        this.TaskManager.EnqueueDelay(1000);
        this.TaskManager.Enqueue(() => Configuration.Instance.UpdateFCData());
        this.TaskManager.Enqueue(() => agentFreeCompany->Hide());
    }

    public void ToggleConfigUi() { } // this.ConfigWindow.Toggle();
    public void ToggleMainUi() => this.MainWindow.Toggle();
}
