using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.UIElements;

namespace net.rs64.TexTransTool.DestructiveTextureUtilities
{
    internal class DestructiveTextureUtilitiesMainMenu : ScriptableObject, TTTMenu.ITTTMenuWindow
    {
        [InitializeOnLoadMethod]
        static void Registering()
        {
            TTTMenu.RegisterMenu(CreateInstance<DestructiveTextureUtilitiesMainMenu>());
        }
        [SerializeField] List<DestructiveUtility> DestructiveUtilityList = new();

        public string MenuName => "DestructiveTextureUtilities";
        public VisualElement CreateGUI()
        {
            var rootVisualElement = new VisualElement();
            foreach (var i in DestructiveUtilityList) { if (i != null) { DestroyImmediate(i); } }
            DestructiveUtilityList.Clear();

            var utilities = AppDomain.CurrentDomain
                .GetAssemblies()
                .First(asm => asm.GetName().Name == "net.rs64.ttt-destructive-texture-utilities")
                .GetTypes()
                .Where(i => !i.IsAbstract)
                .Where(i => typeof(DestructiveUtility).IsAssignableFrom(i));

            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            rootVisualElement.hierarchy.Add(root);

            var utilitiesScrollView = new ScrollView();
            var scrollViewContainer = utilitiesScrollView.Q<VisualElement>("unity-content-container");
            utilitiesScrollView.style.width = 240;
            root.hierarchy.Add(utilitiesScrollView);

            var utilityPanel = new VisualElement();
            utilityPanel.style.width = Length.Percent(100);
            root.hierarchy.Add(utilityPanel);


            foreach (var utilType in utilities)
            {
                var utilityI = CreateInstance(utilType) as DestructiveUtility;
                DestructiveUtilityList.Add(utilityI);

                var button = new Button();
                button.text = utilityI.DisplayName;
                button.clicked += () =>
                {
                    utilityPanel.hierarchy.Clear();
                    utilityI.CreateUtilityPanel(utilityPanel);
                };

                scrollViewContainer.hierarchy.Add(button);
            }

            return rootVisualElement;
        }
    }

    internal abstract class DestructiveUtility : ScriptableObject
    {
        public virtual string DisplayName => GetType().Name;
        public abstract void CreateUtilityPanel(VisualElement rootElement);

        protected PropertyField CreateVIProperyFiled(SerializedProperty serializedProperty)
        {
            var propertyField = new PropertyField();
            propertyField.BindProperty(serializedProperty);
            return propertyField;
        }
    }
}
