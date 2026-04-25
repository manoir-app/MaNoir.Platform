using Home.Common.Messages;

namespace Home.Common;

public delegate MessageResponse MessageHandler(MessageOrigin origin, string topic, string messageBody);