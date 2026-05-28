using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BogChatDesktopClient.Messages;

public class LoginSuccessMessage(string result) : ValueChangedMessage<string>(result);