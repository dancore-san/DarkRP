using Sandbox;
using Sandbox.GameSystems.Player;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Entity.Interactable.Drug
{
    [Library("shroom_drug", Title = "Shroom")]
    public class Shrooms : BaseDrug
    {
        private Bloom bloomEffect;
        private Vignette vignetteEffect;
        private Pixelate pixelateEffect;

        public Shrooms()
        {
            Type = DrugType.Shrooms;
            EntityName = "Shrooms";
            LastingEffect = 20f;  // Shroom-specific lasting effect
            TransitionTime = 100f;
            OverdosePhrases = new List<string> { "tripped too hard on shrooms", "ate a poisonous mushroom" };
            Nicknames = new List<string> { "Magic Mushrooms", "Shrooms" };
            LacedDrugs = new List<string>();
        }

        protected override string GetModelPath()
        {
            return "models/citizen_props/balloonregular01.vmdl"; // Placeholder model path
        }

        protected override void ApplyEffects(Player stats)
        {
            drugStates[Type] = true;

            base.ApplyEffects(stats);
        }


        protected override void ApplyPostProcessingEffects(GameObject player)
        {
            var camera = Scene.Camera;
            if (camera != null)
            {
                bloomEffect = camera.Components.GetOrCreate<Bloom>();
                vignetteEffect = camera.Components.GetOrCreate<Vignette>();
                pixelateEffect = camera.Components.GetOrCreate<Pixelate>();

                // Start the effects interpolation
                _ = InterpolateEffects(true);
                _ = DelayAndResetEffects();
            }
        }

        protected override void ResetPostProcessingEffects()
        {
            var camera = Scene.Camera;
            if (camera != null)
            {
                _ = InterpolateEffects(false);
            }
        }

        private async Task InterpolateEffects(bool onset)
        {
            float duration = TransitionTime;
            float timeElapsed = 0f;

            // Initial and target values for effects
            float initialBloomThreshold = onset ? 0.5f : 0.25f;
            float targetBloomThreshold = onset ? 0.25f : 0.5f;
            float initialBloomStrength = onset ? 0.5f : 1.2f;
            float targetBloomStrength = onset ? 1.2f : 0.5f;

            float initialVignetteIntensity = onset ? 0.0f : 0.5f;
            float targetVignetteIntensity = onset ? 0.5f : 0.0f;
            float initialVignetteSmoothness = onset ? 0.0f : 0.8f;
            float targetVignetteSmoothness = onset ? 0.8f : 0.0f;

            float initialPixelateScale = onset ? 0f : 0.5f;
            float targetPixelateScale = onset ? 0.5f : 0f;

            while (timeElapsed < duration)
            {
                float t = timeElapsed / duration;

                bloomEffect.Threshold = Lerp(initialBloomThreshold, targetBloomThreshold, t);
                bloomEffect.Strength = Lerp(initialBloomStrength, targetBloomStrength, t);

                vignetteEffect.Intensity = Lerp(initialVignetteIntensity, targetVignetteIntensity, t);
                vignetteEffect.Smoothness = Lerp(initialVignetteSmoothness, targetVignetteSmoothness, t);

                pixelateEffect.Scale = Lerp(initialPixelateScale, targetPixelateScale, t);

                timeElapsed += Time.Delta;
                await Task.Yield();
            }

            // Ensure final values are set
            bloomEffect.Threshold = targetBloomThreshold;
            bloomEffect.Strength = targetBloomStrength;
            vignetteEffect.Intensity = targetVignetteIntensity;
            vignetteEffect.Smoothness = targetVignetteSmoothness;
            pixelateEffect.Scale = targetPixelateScale;
        }

        private async Task DelayAndResetEffects()
        {
            // Wait for the duration of the drug's effect
            await Task.Delay((int)(LastingEffect * 1000)); 

            // Reset post-processing effects
            await InterpolateEffects(false);

            drugStates[Type] = false;

            // Destroy the drug entity after effects are reset
            DestroyDrug();
        }

        private float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}