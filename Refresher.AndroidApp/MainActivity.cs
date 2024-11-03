using _Microsoft.Android.Resource.Designer;
using Android.Content;
using LibSceSharp;
using Refresher.Core.Pipelines;

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

        this.AddButtonForPipeline<PS3PatchPipeline>(mainContent, "Patch a PS3 game");
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
        button.Text = "DEBUG: Test LibSceSharp";
        button.SetAllCaps(false);

        button.Click += (_, _) =>
        {
            string initReturn = "Success!";

            try
            {
                using LibSce sce = new();
            }
            catch (Exception ex)
            {
                initReturn = ex.ToString();
            }
            
            new AlertDialog.Builder(this)
                .SetTitle("LibSceSharp returns:")?
                .SetMessage(initReturn)?
                .SetPositiveButton("Hell yeah", (_, _) => {})?
                .SetNegativeButton("FUCK", (_, _) => {})?
                .Show();
        };

        layout.AddView(button);
    }
}