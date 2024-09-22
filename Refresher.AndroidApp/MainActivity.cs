using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Refresher.Core.Pipelines;

namespace Refresher.AndroidApp;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Set our view from the "main" layout resource
        this.SetContentView(ResourceConstant.Layout.activity_main);

        LinearLayout? mainContent = this.FindViewById<LinearLayout>(Resource.Id.MainContent);
        if (mainContent == null)
            throw new Exception("Main content not found");

        this.AddButtonForPipeline<ExamplePipeline>(mainContent);
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
}