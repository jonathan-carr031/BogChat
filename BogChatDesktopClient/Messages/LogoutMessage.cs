using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BogChatDesktopClient.Messages;

public class LogoutMessage(bool logout) : ValueChangedMessage<bool>(logout);