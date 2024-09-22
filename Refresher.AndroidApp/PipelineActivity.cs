using _Microsoft.Android.Resource.Designer;
using Refresher.Core.Pipelines;

namespace Refresher.AndroidApp;

[Activity]
public class PipelineActivity : Activity
{
    private Pipeline? _pipeline;
    
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        this.SetContentView(ResourceConstant.Layout.activity_pipeline);

        this.InitializePipeline();
        
        TextView textView = new(this);
        textView.Text = $"{this._pipeline!.Name}";
        this.SetContentView(textView);
    }

    private void InitializePipeline()
    {
        string? pipelineTypeName = this.Intent?.GetStringExtra("PipelineType");
        if(pipelineTypeName == null)
            throw new Exception("Pipeline type not specified");

        Type? pipelineType = typeof(Pipeline).Assembly.GetType(pipelineTypeName);
        if(pipelineType == null)
            throw new Exception("Pipeline was not found");

        Pipeline pipeline = (Pipeline)Activator.CreateInstance(pipelineType)!;
        this._pipeline = pipeline;
    }
}