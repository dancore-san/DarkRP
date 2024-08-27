using Sandbox;
using Sandbox.GameSystems.Player;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Entity.Interactable.Drug
{
    [Library("lsd_drug", Title = "LSD")]
    public class LSD : BaseDrug
    {
        private Bloom bloomEffect;
        private Sharpen sharpenEffect;
        private ChromaticAberration caEffect;
        private ColorAdjustments colorEffect;
        private FilmGrain filmEffect;
        private Vignette vignetteEffect;
        private Pixelate pixelateEffect;
        private CancellationTokenSource hueRotationCancellation;
        private CancellationTokenSource caEffectScaleCancellation;

        private float currentHueRotate;
        private float currentCAScale;
        private const float frequency = 0.05f;  // Frequency for the sine wave

        public LSD()
        {
            Type = DrugType.LSD;
            EntityName = "LSD";
            LastingEffect = 30f;
            TransitionTime = 200f;
            OverdosePhrases = new List<string> { "tripped too hard on acid", "ate bad acid" };
            Nicknames = new List<string> { "Acid", "LSD" };
            LacedDrugs = new List<string>();
        }

        protected override void OnStart()
        {
            base.OnStart();
            // Log.Info($"LSD LastingEffect final value: {LastingEffect} seconds.");
        }

        protected override string GetModelPath()
        {
            return "models/darkrp_main/drugs/lsd.vmdl";
        }

        protected override void ApplyEffects(Player stats)
        {
            drugStates[Type] = true;

            base.ApplyEffects(stats);
        }

        protected override void HandleHigh(Player stats)
        {
            base.HandleHigh(stats);
            // Log.Info($"{stats.GetPlayerDetails()?.Connection.DisplayName} is now trippin on {EntityName}.");
        }

        protected override void ApplyPostProcessingEffects(GameObject player)
        {
            var camera = Scene.Camera;
            if (camera != null)
            {
                // Log.Info("Camera found. Applying post-processing effects...");

                bloomEffect = camera.Components.GetOrCreate<Bloom>();
                sharpenEffect = camera.Components.GetOrCreate<Sharpen>();
                caEffect = camera.Components.GetOrCreate<ChromaticAberration>();
                colorEffect = camera.Components.GetOrCreate<ColorAdjustments>();
                filmEffect = camera.Components.GetOrCreate<FilmGrain>();
                vignetteEffect = camera.Components.GetOrCreate<Vignette>();
                pixelateEffect = camera.Components.GetOrCreate<Pixelate>();

                // Log.Info("Strong post-processing effects applied.");

                _ = InterpolateEffects(true); // Come Up
                _ = DelayAndResetEffects();   // High + Come Down
            }
        }

        protected override void ResetPostProcessingEffects()
        {
            hueRotationCancellation?.Cancel(); 
            caEffectScaleCancellation?.Cancel();
            var camera = Scene.Camera;
            if (camera != null)
            {
                _ = InterpolateEffects(false); // Come Down
            }
        }

        private async Task InterpolateEffects(bool onset)
        {
            float duration = this.TransitionTime;
            float timeElapsed = 0f;

            // Initialize starting values based on whether it's onset or revert
            float initialBloomThreshold = onset ? 0.5f : bloomEffect.Threshold;
            float targetBloomThreshold = onset ? 0.25f : 0.5f;
            float initialBloomStrength = onset ? 0.5f : bloomEffect.Strength;
            float targetBloomStrength = onset ? 1.2f : 0.5f;

            float initialSharpenScale = onset ? 0.1f : sharpenEffect.Scale;
            float targetSharpenScale = onset ? 0.8f : 0.1f;

            float initialCAScale = currentCAScale;
            float targetCAScale = onset ? 1.0f : 0.0f;
            Vector3 initialCAOffset = onset ? new Vector3(4.0f, 6.0f, 0.0f) : caEffect.Offset;
            Vector3 targetCAOffset = onset ? new Vector3(8.0f, 10.0f, 0.0f) : new Vector3(4.0f, 6.0f, 0.0f);

            float initialColorAdjustmentsSaturation = onset ? 1.0f : colorEffect.Saturation;
            float targetColorAdjustmentsSaturation = onset ? 10.0f : 1.0f;

            float initialFilmIntensity = onset ? 0.0f : filmEffect.Intensity;
            float targetFilmIntensity = onset ? 0.01f : 0.0f;

            float initialHueRotate = currentHueRotate;
            float targetHueRotate = onset ? 360.0f : 0.0f;

            while (timeElapsed < duration)
            {
                float t = timeElapsed / duration;

                // Interpolate all effects
                bloomEffect.Threshold = Lerp(initialBloomThreshold, targetBloomThreshold, t);
                bloomEffect.Strength = Lerp(initialBloomStrength, targetBloomStrength, t);

                sharpenEffect.Scale = Lerp(initialSharpenScale, targetSharpenScale, t);

                caEffect.Scale = Lerp(initialCAScale, targetCAScale, t);
                caEffect.Offset = new Vector3(
                    Lerp(initialCAOffset.x, targetCAOffset.x, t),
                    Lerp(initialCAOffset.y, targetCAOffset.y, t),
                    Lerp(initialCAOffset.z, targetCAOffset.z, t)
                );

                colorEffect.Saturation = Lerp(initialColorAdjustmentsSaturation, targetColorAdjustmentsSaturation, t);

                filmEffect.Intensity = Lerp(initialFilmIntensity, targetFilmIntensity, t);

                colorEffect.HueRotate = Lerp(initialHueRotate, targetHueRotate, t);

                timeElapsed += Time.Delta;
                await Task.Yield();
            }

            currentHueRotate = colorEffect.HueRotate = targetHueRotate % 360;
            currentCAScale = caEffect.Scale = targetCAScale;

            // Start continuous hue rotation and CA scale rotation after initial transition
            if (onset)
            {
                hueRotationCancellation = new CancellationTokenSource();
                _ = ContinuousHueRotation(hueRotationCancellation.Token);

                caEffectScaleCancellation = new CancellationTokenSource();
                _ = ContinuousCAScaleRotation(caEffectScaleCancellation.Token);
            }
        }

        private async Task ContinuousHueRotation(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                currentHueRotate += 2f * Time.Delta;
                if (currentHueRotate > 360f)
                {
                    currentHueRotate -= 360f;
                }

                colorEffect.HueRotate = currentHueRotate;
                await Task.Yield();
            }
        }

        private async Task ContinuousCAScaleRotation(CancellationToken token)
        {
            // Start with the current time that corresponds to the sine wave phase
            float time = MathF.Asin((currentCAScale - 0.5f) / 0.5f) / frequency;

            while (!token.IsCancellationRequested)
            {
                currentCAScale = 0.5f + 0.5f * MathF.Sin(time * frequency);
                caEffect.Scale = currentCAScale;

                time += Time.Delta;
                await Task.Yield();
            }
        }

        private async Task RevertToInitialRotation(float revertDuration)
        {
            hueRotationCancellation?.Cancel();
            caEffectScaleCancellation?.Cancel();

            float initialHueRotate = currentHueRotate;
            float targetHueRotate = 0.0f;

            float initialCAScale = currentCAScale;
            float targetCAScale = 0.0f;

            float timeElapsed = 0f;

            while (timeElapsed < revertDuration)
            {
                float t = timeElapsed / revertDuration;

                // Smoothly transition back to the original hue
                colorEffect.HueRotate = Lerp(initialHueRotate, targetHueRotate, t) % 360;

                // Smoothly transition back to the original CA scale
                caEffect.Scale = Lerp(initialCAScale, targetCAScale, t) % 1.0f;

                timeElapsed += Time.Delta;
                await Task.Yield();
            }

            currentHueRotate = colorEffect.HueRotate = targetHueRotate;
            currentCAScale = caEffect.Scale = targetCAScale;

            // Log.Info("Hue rotation and CA scale reverted to initial state.");
        }

        private async Task DelayAndResetEffects()
        {
            // Log.Info($"Waiting for {LastingEffect} seconds before resetting post-processing effects.");
            await Task.Delay((int)(LastingEffect * 1000)); 
            // Log.Info("Resetting post-processing effects now.");

            await RevertToInitialRotation(TransitionTime);

            // Reset the drug's state to allow reuse
            drugStates[Type] = false;
            Log.Info($"Drug type {Type} state reset to false.");

            DestroyDrug();
        }  

        private float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}