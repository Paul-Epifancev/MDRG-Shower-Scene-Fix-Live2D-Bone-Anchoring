// ==========================================================================
// MDRG — Shower Scene Fix: Live2D Bone Anchoring
// ==========================================================================
// Author: Paul.Epifancev #warlockforbugs (Discord: anon.jun)
// 
// PURPOSE:
// Fixes texture rendering issues in the Shower Scene by anchoring Live2D /
// Cubism bones to world space. Prevents bone hierarchy from distorting
// overridden sprites when TextureOverriderManager applies custom textures.
//
// TECHNICAL OVERVIEW:
// - Uses HarmonyLib to hook into TextureOverriderManager.SetSprite()
// - Detects SpriteRenderers attached to Live2D/Cubism/Bone nodes
// - Detaches them from their parent transform while preserving world
//   position, rotation, and scale (world-space anchoring)
// - Applies Rigidbody/Rigidbody2D kinematic lock to prevent physics drift
//
// REQUIREMENTS:
// - MelonLoader
// - HarmonyLib
// ==========================================================================

using System;
using System.Reflection;
using MelonLoader;
using HarmonyLib;
using UnityEngine;

namespace DebugParser
{
    public static class TextureFixCore
    {
        public static void ApplyTextureAnchor(Component component)
        {
            if (component == null) return;
            try
            {
                GameObject go = component.gameObject;
                if (go == null) return;
                Transform trans = go.transform;
                if (trans == null || trans.parent == null) return;

                string parentName = trans.parent.name;

                // Detect Live2D / Cubism / Bone hierarchy nodes
                if (parentName.Contains("Cubism") || parentName.Contains("Live2D") || parentName.Contains("Bone"))
                {
                    // Preserve world-space transform before reparenting
                    Vector3 worldPos = trans.position;
                    Quaternion worldRot = trans.rotation;
                    Vector3 worldScale = trans.lossyScale;

                    // Detach from parent while keeping world transform intact
                    trans.SetParent(null, true);
                    trans.position = worldPos;
                    trans.rotation = worldRot;
                    trans.localScale = worldScale;

                    // Lock physics bodies to prevent drift
                    var rigidBody = go.GetComponent<Rigidbody>();
                    if (rigidBody != null) rigidBody.isKinematic = true;

                    var rigidBody2D = go.GetComponent<Rigidbody2D>();
                    if (rigidBody2D != null) rigidBody2D.isKinematic = true;
                }
            }
            catch { }
        }
    }

    [HarmonyPatch]
    public static class TextureOverriderSetSpritePatch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("TextureOverriderManager:SetSprite", new Type[] { typeof(string), typeof(Sprite) });
        }

        public static void Postfix(string textureName, Sprite sprite)
        {
            if (sprite == null) return;
            try
            {
                var activeObjects = GameObject.FindObjectsOfType<SpriteRenderer>();
                foreach (var renderer in activeObjects)
                {
                    if (renderer != null && renderer.sprite == sprite)
                    {
                        TextureFixCore.ApplyTextureAnchor(renderer);
                    }
                }
            }
            catch { }
        }
    }
}
