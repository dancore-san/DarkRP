using Sandbox;
using Sandbox.GameSystems.Player;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox.Entity;

namespace Entity.Interactable.Drug
{
    [Library("drug_base", Title = "Drug Base")]
    public partial class BaseDrug : BaseEntity
    {
        public enum DrugType { Shrooms, Cocaine, LSD, Ecstasy, Weed }

        [Property] public virtual DrugType Type { get; set; }

        [Property] public virtual float LastingEffect { get; set; } = 0f;
        [Property] public float TransitionTime { get; set; } = 0f;
        public List<string> LacedDrugs { get; set; } = new List<string>();
        public List<string> Nicknames { get; set; } = new List<string> { "Drugs" };
        public List<string> OverdosePhrases { get; set; } = new List<string> { "took" };

        protected PostProcess postProcess;

        // Static dictionary to keep track of whether each drug type is active
        protected static Dictionary<DrugType, bool> drugStates = new Dictionary<DrugType, bool>();

        protected override void OnStart()
        {
            base.OnStart();
            SetDrugModel();
            SetDrugColliderModel();
        }

        protected virtual void SetDrugModel()
        {
            var renderer = GameObject.Components.Get<ModelRenderer>();
            if (renderer != null)
            {
                renderer.Model = Model.Load(GetModelPath());
            }
        }

        protected virtual string GetModelPath()
        {
            return "models/dev/box.vmdl"; // Default model path
        }

        protected virtual void SetDrugColliderModel()
        {
            var collider = GameObject.Components.Get<ModelCollider>();
            if (collider != null)
            {
                collider.Model = Model.Load(GetModelPath());
            }
        }

        public override void InteractUse(SceneTraceResult tr, GameObject player)
        {
            base.InteractUse(tr, player);

            if (player.Components.Get<Player>() is Player playerStats)
            {
                Log.Info($"Before interaction for {Type}: drugStates[{Type}] = {drugStates.GetValueOrDefault(Type, false)}");

                if (drugStates.ContainsKey(Type) && drugStates[Type])
                {
                    Log.Info($"Cannot use {Type}: already high.");
                    return;
                }

                drugStates[Type] = true;
                Log.Info($"Consuming drug: {Type}. drugStates[{Type}] set to true.");

                ApplyEffects(playerStats);
                HandleHigh(playerStats);
                ApplyPostProcessingEffects(player);
                _ = DelayAndResetEffects();
                ApplyLacedDrugs(playerStats);
                OnConsumed();
            }
        }

        protected virtual void ApplyEffects(Player player)
        {
        }

        protected virtual void HandleHigh(Player player)
        {
        }

        protected virtual void ApplyLacedDrugs(Player player)
        {
            foreach (var drug in LacedDrugs)
            {
                var lacedDrugType = TypeLibrary.GetType(drug);

                if (lacedDrugType != null && typeof(BaseDrug).IsAssignableFrom(lacedDrugType.TargetType))
                {
                    var lacedDrug = lacedDrugType.Create<BaseDrug>();

                    if (!drugStates.ContainsKey(lacedDrug.Type) || !drugStates[lacedDrug.Type])
                    {
                        drugStates[lacedDrug.Type] = true;

                        lacedDrug.ApplyEffects(player);
                        lacedDrug.HandleHigh(player);
                        lacedDrug.OnConsumed();
                    }
                    else
                    {
                        Log.Info($"Laced drug {lacedDrug.Type} is already active, skipping application.");
                    }
                }
            }
        }

        protected virtual void OnConsumed()
        {
            Enabled = false;
            HideDrug();
        }

        protected virtual void ApplyPostProcessingEffects(GameObject player)
        {
        }

        private async Task DelayAndResetEffects()
        {
            await Task.Delay((int)(LastingEffect * 1000));
            ResetPostProcessingEffects();
        }

        protected virtual void ResetPostProcessingEffects()
        {
            if (postProcess != null)
            {
                postProcess = null;
            }

            drugStates[Type] = false;
            Log.Info($"Drug type {Type} state reset to false.");
        }

        [Broadcast]
        private void HideDrug()
        {
            var renderer = GameObject.Components.Get<ModelRenderer>();
            if (renderer != null)
            {
                renderer.Enabled = false;
            }
            var collider = GameObject.Components.Get<ModelCollider>();
            if (collider != null)
            {
                collider.Enabled = false;
            }
        }

        [Broadcast]
        public void DestroyDrug()
        {
            this.GameObject.Destroy();
        }
    }
}