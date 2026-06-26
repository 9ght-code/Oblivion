using System;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Oblivion.GUI.Services;

public class NotificationService(ISnackbarService snackbarService)
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(3);

    public void Info(string title, string message)
        => snackbarService.Show(title, message, ControlAppearance.Info, null, DefaultDuration);

    public void Success(string title, string message)
        => snackbarService.Show(title, message, ControlAppearance.Success, null, DefaultDuration);

    public void Warning(string title, string message)
        => snackbarService.Show(title, message, ControlAppearance.Caution, null, DefaultDuration);

    public void Error(string title, string message)
        => snackbarService.Show(title, message, ControlAppearance.Danger, null, DefaultDuration);
}
