using Refresher.AndroidApp.Logging;
using Refresher.Core;
using Refresher.Core.Logging;

namespace Refresher.AndroidApp;

public abstract class RefresherActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        RefresherState.InitializeLogger([new AndroidSink(), new EventSink(), new SentryBreadcrumbSink()]);
    }
}