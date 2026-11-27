using Game.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Core.Tests
{
    public class UnitDefinitionAssetTests
    {
        [Test]
        public void ToDefinition_CopiesSerializedValues()
        {
            var asset = ScriptableObject.CreateInstance<UnitDefinitionAsset>();
            try
            {
                var serialized = new SerializedObject(asset);
                serialized.FindProperty("maxHealth").intValue = 250;
                serialized.FindProperty("removeOnDowned").boolValue = true;
                serialized.FindProperty("autoRevive").boolValue = true;
                serialized.FindProperty("autoReviveDelay").floatValue = 7.5f;
                serialized.FindProperty("revivePoint").enumValueIndex = (int)RevivePointRule.TeamRevivePoint;
                serialized.FindProperty("reviveHealthRatio").floatValue = 0.4f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var definition = asset.ToDefinition();

                Assert.AreEqual(250, definition.MaxHealth);
                Assert.IsTrue(definition.RemoveOnDowned);
                Assert.IsTrue(definition.AutoRevive);
                Assert.AreEqual(7.5f, definition.AutoReviveDelay);
                Assert.AreEqual(RevivePointRule.TeamRevivePoint, definition.RevivePoint);
                Assert.AreEqual(0.4f, definition.ReviveHealthRatio);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void ToDefinition_DefaultAsset_IsValid()
        {
            var asset = ScriptableObject.CreateInstance<UnitDefinitionAsset>();
            try
            {
                var definition = asset.ToDefinition();

                Assert.AreEqual(100, definition.MaxHealth);
                Assert.IsFalse(definition.RemoveOnDowned);
                Assert.IsFalse(definition.AutoRevive);
                Assert.AreEqual(RevivePointRule.DownedPosition, definition.RevivePoint);
                Assert.AreEqual(100, definition.ReviveHealth);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
