using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CugaCalibration.Core.Models;
using CugaCalibration.ViewModels;
using CugaCalibration.ViewModels.Common.Windows.View;
using CugaScript.Core.Models;
using ICSharpCode.AvalonEdit.Document;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helper.File;
using Net.Utilities.Helper.IOC.Providers;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Events;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.IO;
using LogLevel = Net.Utilities.Enums.LogLevel;
using ScriptFileMenu = CugaScript.Core.Models.ScriptFileMenu;

namespace CugaScript.ViewModels;

[IOCAppService(ServiceType = typeof(MainWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MainWindowViewModel(
    ApplicationCookie applicationCookie,
    IOptions<ApplicationSetting> options,
    IDialogWindowProvider dialogWindowProvider,
    IWindowManagerService windowManagerService,
    ISynchronizationContextProvider synchronizationContextProvider,
    IMessenger messenger,
    ILogger<MainWindowViewModel> logger)
    : ViewModelBase
{
    [ObservableProperty]
    private LogLevel _minLogLevel = LogLevel.Info;

    [ObservableProperty]
    private bool _isLoadingOk;

    [ObservableProperty]
    private bool _isRunningScript;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private bool _isEditDocument;

    [ObservableProperty]
    private TextDocument _textDocument = new();

    [ObservableProperty]
    private ScriptFileMenu _scriptFileMenu = new();

    [ObservableProperty]
    private ScriptFileMenu? _selectScriptFileMenu;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextDocument), nameof(Title))]
    private ScriptFileMenu? _activeItem;

    public string Title => ActiveItem is not null ? $"{applicationCookie.Title} {ActiveItem.Name} {(IsEditDocument ? "*" : string.Empty)}" : $"{applicationCookie.Title} {(IsEditDocument ? "*" : string.Empty)}";

    partial void OnActiveItemChanged(ScriptFileMenu? value)
    {
        var content = value is not null ? File.ReadAllText(value.FilePath) : string.Empty;

        synchronizationContextProvider.Send(() =>
        {
            TextDocument.Text = content;
            IsEditDocument = false;
        });
    }

    #region Command

    [RelayCommand]
    private void Loaded()
    {
        IsLoadingOk = false;

        var showDialog = windowManagerService.ShowDialog(HostApplication.GetRequiredService<LoginWindowViewModel>());
        if (showDialog == false)
        {
            return;
        }

        showDialog = windowManagerService.ShowDialog(HostApplication.GetRequiredService<LoadingWindowViewModel>());
        if (showDialog == false)
        {
            return;
        }

        windowManagerService.ShowWindow(HostApplication.GetRequiredService<StageWindowViewModel>());
        windowManagerService.ShowWindow(HostApplication.GetRequiredService<MicroscopeWindowViewModel>());

        TextDocument.TextChanged += (_, _) => IsEditDocument = true;

        OnPropertyChanged(nameof(Title));
        IsLoadingOk = true;
        LoadScriptMenu();
    }

    #region ToolBar

    [RelayCommand]
    private async Task NewScriptFileAsync()
    {
        await InvokeAsync(async () =>
        {
            await Task.CompletedTask.ConfigureAwait(false);

            var directoryPath = SelectScriptFileMenu?.DirectoryPath ?? ScriptFileMenu.DirectoryPath;

            var inputNameWindowViewModel = new InputNameWindowViewModel();
            if (windowManagerService.ShowDialog(inputNameWindowViewModel) == false) return;

            var filePath = Path.Combine(directoryPath, $"{FileHelper.RemoveInvalidFileName(inputNameWindowViewModel.Name)}.cs");
            if (File.Exists(filePath))
            {
                dialogWindowProvider.ShowDialog("The file already exists!", dialogIconEnum: DialogIconEnum.Warning);
                return;
            }

            FileHelper.CreateFile(filePath);
        }, "New Script File").ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task NewScriptDirectoryAsync()
    {
        await InvokeAsync(async () =>
        {
            await Task.CompletedTask.ConfigureAwait(false);

            var directoryPath = SelectScriptFileMenu?.DirectoryPath ?? ScriptFileMenu.DirectoryPath;

            var inputNameWindowViewModel = new InputNameWindowViewModel();
            if (windowManagerService.ShowDialog(inputNameWindowViewModel) == false) return;

            var directoryName = Path.Combine(directoryPath, $"{FileHelper.RemoveInvalidFileName(inputNameWindowViewModel.Name)}");
            if (Directory.Exists(directoryName))
            {
                dialogWindowProvider.ShowDialog("The directory already exists!", dialogIconEnum: DialogIconEnum.Warning);
                return;
            }

            DirectoryHelper.CreateDirectoryIfNotExists(directoryName);
        }, "New Script Directory").ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task DeleteDirectoryAsync()
    {
        await InvokeAsync(async () =>
        {
            await Task.CompletedTask.ConfigureAwait(false);

            if (SelectScriptFileMenu is null)
            {
                dialogWindowProvider.ShowDialog("Please select a item to delete it!", dialogIconEnum: DialogIconEnum.Warning);
                return;
            }

            if (SelectScriptFileMenu.IsDirectory)
            {
                if (SelectScriptFileMenu.ChildList.Count > 0)
                {
                    dialogWindowProvider.ShowDialog("Please delete the child items first!", dialogIconEnum: DialogIconEnum.Warning);
                    return;
                }

                Directory.Delete(SelectScriptFileMenu.DirectoryPath, true);
            }
            else
            {
                if (dialogWindowProvider.TryShowDialog($"Delete this file {SelectScriptFileMenu.Name}!", out var dialogResultEnum, DialogButtonsEnum.OKCancel, DialogIconEnum.Warning) == true && dialogResultEnum == DialogResultEnum.OK)
                {
                    File.Delete(SelectScriptFileMenu.FilePath);
                }
            }
        }, "Delete").ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SaveScriptFileAsync()
    {
        await InvokeAsync(async () =>
        {
            await Task.CompletedTask.ConfigureAwait(false);

            if (ActiveItem is null)
            {
                dialogWindowProvider.ShowDialog("Please open script file to save it!", dialogIconEnum: DialogIconEnum.Warning);
                return;
            }

            var textDocumentText = string.Empty;
            synchronizationContextProvider.Send(() => textDocumentText = TextDocument.Text);
            File.WriteAllText(ActiveItem.FilePath, textDocumentText);

            IsEditDocument = false;
        }, "Save Script", true).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task OpenScriptFileAsync(ScriptFileMenu scriptFileMenu)
    {
        await InvokeAsync(async () =>
        {
            await Task.CompletedTask.ConfigureAwait(false);

            if (scriptFileMenu.Equals(ActiveItem)) return;

            ActiveItem = scriptFileMenu;
        }, "Load Script").ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task CloseScriptFileAsync()
    {
        await InvokeAsync(async () =>
        {
            await Task.CompletedTask.ConfigureAwait(false);
            SelectScriptFileMenu = null;
            ActiveItem = null;
        }, "Clos Script").ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task RunScriptFile(CancellationToken cancellationToken)
    {
        await InvokeAsync(async () =>
        {
            if (ActiveItem is null)
            {
                dialogWindowProvider.ShowDialog("Please open script file to run it!", dialogIconEnum: DialogIconEnum.Warning);
                return;
            }

            var textDocumentText = string.Empty;
            synchronizationContextProvider.Send(() => textDocumentText = TextDocument.Text);
            IsRunningScript = true;
            var scriptParameter = HostApplication.GetRequiredService<ScriptParameter>();
            try
            {
                messenger.Send(PopupWindowEventFactory.DisableIsPopupWindowEnable());
                var scriptOptions = ScriptOptions.Default
                    .WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic == false && string.IsNullOrWhiteSpace(assembly.Location) == false))
                    .WithImports(
                        "System",
                        "System.Collections.Generic",
                        "System.IO",
                        "System.Linq",
                        "System.Net.Http",
                        "System.Threading",
                        "System.Threading.Tasks"
                    );

                scriptParameter.CancellationToken = cancellationToken;
                await CSharpScript.RunAsync(textDocumentText, scriptOptions, scriptParameter, scriptParameter.GetType(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException) throw;

                logger.LogWarning("{@Name}: Run Script Canceled!", nameof(MainWindowViewModel));
            }
            finally
            {
                IsRunningScript = false;
                scriptParameter.CancellationToken = default;
                messenger.Send(PopupWindowEventFactory.EnableIsPopupWindowEnable());
            }
        }, "Run Script").ConfigureAwait(false);
    }

    #endregion ToolBar

    #region Menu

    [RelayCommand]
    private void ShowStage()
    {
        var stageWindowViewModel = HostApplication.GetRequiredService<StageWindowViewModel>();
        if (stageWindowViewModel.Show() == false) windowManagerService.ShowWindow(stageWindowViewModel);
    }

    [RelayCommand]
    private void ShowMicroscope()
    {
        var microscopeWindowViewModel = HostApplication.GetRequiredService<MicroscopeWindowViewModel>();
        if (microscopeWindowViewModel.Show() == false) windowManagerService.ShowWindow(microscopeWindowViewModel);
    }

    #endregion Menu

    #endregion Command

    private async Task InvokeAsync(Func<Task> func, string log, bool isSaveDocument = false)
    {
        try
        {
            await Task.Run(async () =>
            {
                if (IsRunningScript)
                {
                    dialogWindowProvider.ShowDialog("Please wait for the current script to finish running!", dialogIconEnum: DialogIconEnum.Warning);
                    return;
                }

                if (isSaveDocument == false && IsEditDocument)
                {
                    dialogWindowProvider.ShowDialog("The current script has been modified!", dialogIconEnum: DialogIconEnum.Warning);
                    return;
                }

                await func.Invoke().ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: {@Log} Failed", nameof(MainWindowViewModel), log);
        }
        finally
        {
            LoadScriptMenu();
        }
    }

    private void LoadScriptMenu()
    {
        DirectoryHelper.CreateDirectoryIfNotExists(options.Value.ScriptDirectory);

        ScriptFileMenu.Name = new DirectoryInfo(options.Value.ScriptDirectory).Name;
        ScriptFileMenu.DirectoryPath = options.Value.ScriptDirectory;
        ScriptFileMenu.FilePath = string.Empty;
        ScriptFileMenu.ChildList = [];
        SelectScriptFileMenu = null;

        var scriptFileMenu = new ScriptFileMenu(ScriptFileMenu);
        BuildMenuTree(scriptFileMenu);
        ScriptFileMenu.ChildList = scriptFileMenu.ChildList;

        return;

        static void BuildMenuTree(ScriptFileMenu temp)
        {
            temp.ChildList = [];
            var di = new DirectoryInfo(temp.DirectoryPath);
            var childDirectories = di.GetDirectories();

            foreach (var filePath in di.GetFiles("*.cs")
                         .OrderBy(fileInfo => fileInfo.Name)
                         .Select(fileInfo => fileInfo.FullName))
            {
                temp.ChildList.Add(new ScriptFileMenu
                {
                    Name = Path.GetFileName(filePath),
                    DirectoryPath = Path.GetDirectoryName(filePath)!,
                    FilePath = filePath,
                    ChildList = []
                });
            }

            if (childDirectories.Length <= 0) return;

            foreach (var t in childDirectories)
            {
                var childMenu = new ScriptFileMenu
                {
                    Name = t.Name,
                    DirectoryPath = t.FullName,
                    FilePath = string.Empty,
                    ChildList = []
                };
                BuildMenuTree(childMenu);
                temp.ChildList.Add(childMenu);
            }
        }
    }
}