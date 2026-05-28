using System;
using System.Threading.Tasks;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.Messages;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace BogChatDesktopClient.ViewModels;

public partial class LoginPageViewModel : PageViewModel {
    private IMessenger _messenger;

    public LoginPageViewModel(IMessenger messenger) {
        PageName = PageNames.LoginPage;

        _messenger = messenger;
    }

    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ErrorMessage { get; set; }

    [RelayCommand]
    public async Task LoginCommand() {
        Console.WriteLine("User is Logging in!");

        if (string.IsNullOrWhiteSpace(Username)) {
            ErrorMessage = "Username cannot be blank";
            return;
        }

        DataSaver.SaveData(Username);

        await Task.Delay(1000);

        _messenger.Send(new LoginSuccessMessage(Username));
    }
}