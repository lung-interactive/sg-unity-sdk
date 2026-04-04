using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SGUnitySDK.Editor.Core.Singletons;
using SGUnitySDK.Editor.Presentation.Elements;
using UnityEngine;

namespace SGUnitySDK.Editor.Presentation.Elements
{
    public class DevelopmentStepsElementFactory
    {
        private Dictionary<DevelopmentStep, DevelopmentStepElement> _elements = new();

        public DevelopmentStepsElementFactory()
        {
            Type developmentStepElementType = typeof(DevelopmentStepElement);
            var stepElementTypes = developmentStepElementType.Assembly.GetTypes()
                .Where(t => t.IsClass &&
                           !t.IsAbstract &&
                           t.IsSubclassOf(developmentStepElementType));

            foreach (var type in stepElementTypes)
            {
                try
                {
                    var instance = (DevelopmentStepElement)Activator.CreateInstance(type);

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

        public bool TryGet(DevelopmentStep step, out DevelopmentStepElement element)
        {
            return _elements.TryGetValue(step, out element);
        }
    }
}