using UnityEngine;
using System;

namespace CC
{
    public class Layer
    {
        // Layers are used for raycast and physics collision logics
        private const string NameUI = "UI";
        private const string NamePlayer = "Player";
        // This Layer does not interfact with itself
        private const string NameCharacter = "Character";
        private const string NameTerrain = "Terrain";
        private const string NameBounds = "Bounds";
        private const string NameBox = "Box";
        private const string NameWeapon = "Weapon";
        private const string NameExplosion = "Explosion";
        private const string NamePlatform = "Platform";
        private const string NamePositioning = "Positioning";
        // This Layer does not interact with character
        private const string NameWeaponDropable = "WeaponDropable";
        // This layer only interact with Weapon and WeaponDropable
        private const string NameShield = "Shield";
        private const string NameMine = "Mine";
        private const string NameObject = "Object";
        private const string NameTransferForce = "TransferForce";
        // This Layer contains nothing (used in position on map to remove all the layer constraints)
        private const string NameNothing = "Nothing";

        public static Layer Character => Layer.FromName(NameCharacter);
        public static Layer Box => Layer.FromName(NameBox);
        public static Layer Terrain => Layer.FromName(NameTerrain);
        public static Layer Bounds => Layer.FromName(NameBounds);
        public static Layer Platform => Layer.FromName(NamePlatform);
        public static Layer Mine => Layer.FromName(NameMine);
        public static Layer Object => Layer.FromName(NameObject);
        public static Layer Nothing => Layer.FromName(NameNothing);
        public static Layer Positioning => Layer.FromName(NamePositioning);
        public static Layer WeaponDropable => Layer.FromName(NameWeaponDropable);
        public static Layer Weapon => Layer.FromName(NameWeapon);
        public static Layer Shield => Layer.FromName(NameShield);
        public static Layer TransferForce => Layer.FromName(NameTransferForce);

        public string Name { get; private set; }
        public int Index {
            get {
                if (index == -1) {
                    throw new Exception("Unvalid Index in Layer, it is probably recombined.");
                }
                return index;
            }
            private set {
                index = value;
            }
        }
        public int Mask { get; private set; }
        public ContactFilter2D ContactFilter => new ContactFilter2D {
            layerMask = Mask,
            useLayerMask = true
        };
        /// <summary>
        /// Return a Layer.Mask that represent everything except the current Layer
        /// doc: https://docs.unity3d.com/Manual/Layers.html
        /// </summary>
        public int ExceptThatMask => ~Mask;
        public ContactFilter2D ExceptThatContactFilter => new ContactFilter2D {
            layerMask = ExceptThatMask,
            useLayerMask = true
        };

        private int index;

        private static Layer FromName(string name)
        {
            return new Layer {
                Name = name,
                Index = LayerMask.NameToLayer(name),
                Mask = 1 << LayerMask.NameToLayer(name)
            };
        }

        public static Layer operator |(Layer a, Layer b)
        {
            return new Layer {
                Name = a.Name + " or " + b.Name,
                Index = -1,
                Mask = a.Mask | b.Mask
            };
        }

        public override string ToString()
        {
            return "Layer name: " + Name + " index: " + index + " mask: " + Mask;
        }
    }
}
