using UnityEngine;
using UnityEngine.UIElements;

namespace SGUnitySDK.Http
{
    public static class ErrorMessageUSSHelper
    {
        /// <summary>
        /// Cria um VisualElement estilizado para exibir mensagens de erro
        /// </summary>
        public static VisualElement CreateErrorElement(ErrorMessageBag errorBag)
        {
            var container = new VisualElement();
            container.AddToClassList("error-container");

            var titleLabel = new Label($"Operation Errors: ");
            titleLabel.AddToClassList("error-title");
            container.Add(titleLabel);

            var listContainer = new VisualElement();
            listContainer.AddToClassList("error-list");

            foreach (var message in errorBag.Messages)
            {
                var errorItem = new Label(message);
                errorItem.AddToClassList("error-item");
                listContainer.Add(errorItem);
            }

            container.Add(listContainer);
            return container;
        }
    }
}