using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Refresher.AndroidApp.Logging;
using Refresher.Core;
using Refresher.Core.Logging;
using Refresher.Core.Pipelines;
using SCEToolSharp;

using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;

namespace Refresher.AndroidApp;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        State.InitializeLogger([new AndroidSink(), new EventSink(), new SentryBreadcrumbSink()]);

        // Set our view from the "main" layout resource
        this.SetContentView(ResourceConstant.Layout.activity_main);

        LinearLayout? mainContent = this.FindViewById<LinearLayout>(ResourceConstant.Id.MainContent);
        if (mainContent == null)
            throw new Exception("Main content not found");

        this.AddButtonForPipeline<ExamplePipeline>(mainContent);
        this.AddSceToolSharpTestButton(mainContent);
    }

    private void AddButtonForPipeline<TPipeline>(LinearLayout layout) where TPipeline : Pipeline
    {
        Button button = new(this);
        button.Text = typeof(TPipeline).Name;

        button.Click += (_, _) =>
        {
            Toast.MakeText(this, button.Text, ToastLength.Short)?.Show();
            Intent intent = new(this, typeof(PipelineActivity));
            
            intent.PutExtra("PipelineType", typeof(TPipeline).FullName);

            this.StartActivity(intent);
        };

        layout.AddView(button);
    }

    [Conditional("DEBUG")]
    private void AddSceToolSharpTestButton(LinearLayout layout)
    {
        Button button = new(this);
        button.Text = "DEBUG: Test LibSceToolSharp";

        button.Click += (_, _) =>
        {
            string initReturn;

            try
            {
                initReturn = LibSceToolSharp.Init().ToString();
            }
            catch (Exception ex)
            {
                initReturn = ex.ToString();
            }
            
            new AlertDialog.Builder(this)
                .SetTitle("LibSceToolSharp.Init() returns")?
                .SetMessage(initReturn)?
                .SetPositiveButton("Hell yeah", (_, _) => {})?
                .SetNegativeButton("FUCK", (_, _) => {})?
                .Show();
        };

        layout.AddView(button);
    }
}