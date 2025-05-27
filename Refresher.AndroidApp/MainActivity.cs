using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Refresher.Core.Pipelines;
using Refresher.Core.Pipelines.Lbp;
using SCEToolSharp;

using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;

namespace Refresher.AndroidApp;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : RefresherActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Set our view from the "main" layout resource
        this.SetContentView(ResourceConstant.Layout.activity_main);

        LinearLayout? mainContent = this.FindViewById<LinearLayout>(ResourceConstant.Id.MainContent);
        if (mainContent == null)
            throw new Exception("Main content not found");

        this.AddButtonForPipeline<LbpPS3PatchPipeline>(mainContent, "Patch LBP1/2/3 for PS3");
        this.AddButtonForPipeline<PS3PatchPipeline>(mainContent, "Patch any PS3 game");
        #if DEBUG
        this.AddButtonForPipeline<ExamplePipeline>(mainContent, "Example Pipeline");
        this.AddSceToolSharpTestButton(mainContent);
        #endif
    }

    private void AddButtonForPipeline<TPipeline>(LinearLayout layout, string name) where TPipeline : Pipeline
    {
        Button button = new(this);
        button.Text = name;
        button.SetAllCaps(false);

        button.Click += (_, _) =>
        {
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
        button.SetAllCaps(false);

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