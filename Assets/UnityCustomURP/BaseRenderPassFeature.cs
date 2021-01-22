/**
 * @license
 * This project are available under the zlib license:
 *
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * varising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

/**
 * @fileoverview BaseRenderPassFeature.cs
 *
 * A simple render pass feature.
 *
 * @author Alexandre Ribeiro de Sá (@alexribeirodesa)
 */

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BaseRenderPassFeature : ScriptableRendererFeature {
    class CustomRenderPass : ScriptableRenderPass {
        public RenderTargetIdentifier source;
        private RenderTargetHandle tempRenderTargetHandler;
        private Material material;

        public CustomRenderPass(Material material) {
            this.material = material;
            tempRenderTargetHandler.Init("_TemporaryColorTexture");
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            // This method is called before executing the render pass.
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in an performance manner.
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.

            CommandBuffer commandBuffer = CommandBufferPool.Get();

            commandBuffer.GetTemporaryRT(tempRenderTargetHandler.id, renderingData.cameraData.cameraTargetDescriptor);
            Blit(commandBuffer, source, tempRenderTargetHandler.Identifier(), material);
            Blit(commandBuffer, tempRenderTargetHandler.Identifier(), source);

            context.ExecuteCommandBuffer(commandBuffer);
            CommandBufferPool.Release(commandBuffer);
        }

        public override void FrameCleanup(CommandBuffer cmd) {
            /// Cleanup any allocated resources that were created during the execution of this render pass.
        }
    }

    public Material material = null;        // The material we will use as a filter.
    CustomRenderPass m_ScriptablePass;

    public override void Create() {
        m_ScriptablePass = new CustomRenderPass(this.material);
        m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        // Disable the filter render inside the editor window.
        if (renderingData.cameraData.camera.name == "SceneCamera" || renderingData.cameraData.camera.name == "Preview Scene Camera") {
            return;
        }

        m_ScriptablePass.source = renderer.cameraColorTarget;
        renderer.EnqueuePass(m_ScriptablePass);
    }
}