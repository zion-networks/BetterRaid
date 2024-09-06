using System;
using Microsoft.Extensions.Logging;

namespace BetterRaid.ViewModels;

public class AddChannelWindowViewModel : ViewModelBase
{
    public AddChannelWindowViewModel(ILogger<AddChannelWindowViewModel> logger)
    {
        logger.LogDebug("AddChannelWindowViewModel created");
    }
}