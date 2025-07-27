using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SGUnitySDK.Editor
{
    public class VersioningStepsElementFactory
    {
        private Dictionary<VersioningStep, VersioningStepElement> _elements = new();

        public VersioningStepsElementFactory()
        {
            Type versioningStepElementType = typeof(VersioningStepElement);
            var stepElementTypes = versioningStepElementType.Assembly.GetTypes()
                .Where(t => t.IsClass &&
                           !t.IsAbstract &&
                           t.IsSubclassOf(versioningStepElementType));

            foreach (var type in stepElementTypes)
            {
                try
                {
                    var instance = (VersioningStepElement)Activator.CreateInstance(type);

                    if (instance != null)
                    {
                        _elements.Add(instance.Step, instance);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create instance of {type.Name}");
                    Debug.LogException(ex);
                }
            }
        }

        public bool TryGet(VersioningStep step, out VersioningStepElement element)
        {
            return _elements.TryGetValue(step, out element);
        }
    }
}