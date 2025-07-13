using UnityEngine;

public class Glitch : MonoBehaviour
{
    #region Variables
    public Shader SCShader;
    private float TimeX = 1.0f;
    private Material SCMaterial;

    [Range(0, 1)]
    public float glitch = 1.0f;
    #endregion

    #region Properties
    private Material MaterialInstance
    {
        get
        {
            if (SCMaterial == null && SCShader != null)
            {
                SCMaterial = new Material(SCShader) { hideFlags = HideFlags.HideAndDontSave };
            }
            return SCMaterial;
        }
    }
    #endregion

    private void Start()
    {
        if (SCShader == null)
        {
            SCShader = Shader.Find("Custom/Glitch");
        }

        if (SCShader == null)
        {
            enabled = false;
            return;
        }

        SCMaterial = MaterialInstance; 
    }

    private void OnRenderImage(RenderTexture sourceTexture, RenderTexture destTexture)
    {
        if (SCMaterial != null)
        {
            TimeX += Time.deltaTime;
            if (TimeX > 100) TimeX = 0;

            SCMaterial.SetFloat("_TimeX", TimeX);
            SCMaterial.SetFloat("_Glitch", glitch);
            SCMaterial.SetVector("_ScreenResolution", new Vector4(sourceTexture.width, sourceTexture.height, 0.0f, 0.0f));

            Graphics.Blit(sourceTexture, destTexture, SCMaterial);
        }
        else
        {
            Graphics.Blit(sourceTexture, destTexture);
        }
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying)
        {
            ResetShaderInEditor();
        }
    }

    private void ResetShaderInEditor()
    {
        SCShader = Shader.Find("Custom/Glitch");
    }
#endif

    private void OnDisable()
    {
        if (SCMaterial != null)
        {
            DestroyImmediate(SCMaterial);
            SCMaterial = null;
        }
    }
    public void Destroy()
    {
        Destroy(this);
    }
}
