using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.UIElements;

#if SHADER_GRAPH_HOTKEY

#else
public class MaterialGraphView
{
}

public class GraphData
{
}

public class AbstractMaterialNode
{
    
}
#endif

namespace NKStudio.ShaderGraph.HotKey
{
    internal class KeyboardHotKey
    {
        private const string NodeAssetPath = "Assets/Settings/Node Controls.inputactions";
        private MaterialGraphView GraphView { get; }
        private GraphData GraphData { get; }

        private string _currentNode = string.Empty;

#if ENABLE_INPUT_SYSTEM
        private InputActionAsset _inputActionAsset;
#endif
        public KeyboardHotKey(MaterialGraphView graphView, GraphData graphData)
        {
            GraphView = graphView;
            GraphData = graphData;

#if SHADER_GRAPH_HOTKEY
            GraphView.RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
            GraphView.RegisterCallback<KeyUpEvent>(OnKeyUpEvent);
            GraphView.RegisterCallback<MouseDownEvent>(OnMouseDownEvent, TrickleDown.TrickleDown);
            _inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(NodeAssetPath);
#endif
        }

#if SHADER_GRAPH_HOTKEY
        private void OnMouseDownEvent(MouseDownEvent evt)
        {
            if (_inputActionAsset == null)
                _inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(NodeAssetPath);

            if (_inputActionAsset == null) return;

            Vector2 mousePosition = evt.originalMousePosition;

            if (_currentNode == null)
            {
                return;
            }

            try
            {
                Type targetType = Type.GetType($"UnityEditor.ShaderGraph.{_currentNode}, Unity.ShaderGraph.Editor");

                if (targetType == null) return;

                AbstractMaterialNode currentTypeNode = Activator.CreateInstance(targetType) as AbstractMaterialNode;

                CreateNode(() => currentTypeNode, mousePosition);
            }
            catch (Exception e)
            {
                Debug.LogError(e + $"{_currentNode}");
                throw;
            }
        }

        private void OnKeyUpEvent(KeyUpEvent evt)
        {
            _currentNode = string.Empty;
        }

        private void OnKeyDownEvent(KeyDownEvent evt)
        {
            if (_inputActionAsset == null)
                _inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(NodeAssetPath);

            if (_inputActionAsset == null) return;

            string evtPath = KeycodeToPath(evt.keyCode);

            if (evtPath.Length == 0) return;

            foreach (InputBinding binding in from actionMap in _inputActionAsset.actionMaps
                     from action in actionMap.actions
                     from binding in action.bindings
                     where binding.path == evtPath
                     select binding)
            {
                _currentNode = binding.action;
                return;
            }
        }

        private static string KeycodeToPath(KeyCode keyCode)
        {
            const string kDefaultKey = "<Keyboard>";

            if ((int) keyCode >= 48 && (int) keyCode <= 57)
            {
                int formatInt = (int) keyCode - 48;
                return $"{kDefaultKey}/{formatInt}";
            }

            if ((int) keyCode >= 97 && (int) keyCode <= 122)
            {
                string formatString = keyCode.ToString().ToLower();
                return $"{kDefaultKey}/{formatString}";
            }

            switch (keyCode)
            {
                case KeyCode.Semicolon:
                    return $"{kDefaultKey}/semicolon";
                case KeyCode.Slash:
                    return $"{kDefaultKey}/slash";
                case KeyCode.Backslash:
                    return $"{kDefaultKey}/backslash";
                case KeyCode.Comma:
                    return $"{kDefaultKey}/comma";
                case KeyCode.Equals:
                    return $"{kDefaultKey}/equals";
                case KeyCode.F1:
                    return $"{kDefaultKey}/f1";
                case KeyCode.F2:
                    return $"{kDefaultKey}/f2";
                case KeyCode.F3:
                    return $"{kDefaultKey}/f3";
                case KeyCode.F4:
                    return $"{kDefaultKey}/f4";
                case KeyCode.F5:
                    return $"{kDefaultKey}/f5";
                case KeyCode.F6:
                    return $"{kDefaultKey}/f6";
                case KeyCode.F7:
                    return $"{kDefaultKey}/f7";
                case KeyCode.F8:
                    return $"{kDefaultKey}/f8";
                case KeyCode.F9:
                    return $"{kDefaultKey}/f9";
                case KeyCode.F10:
                    return $"{kDefaultKey}/f10";
                case KeyCode.F11:
                    return $"{kDefaultKey}/f11";
                case KeyCode.F12:
                    return $"{kDefaultKey}/f12";
                case KeyCode.F13:
                    return $"{kDefaultKey}/f13";
                case KeyCode.F14:
                    return $"{kDefaultKey}/f14";
                case KeyCode.F15:
                    return $"{kDefaultKey}/f15";
                case KeyCode.Minus:
                    return $"{kDefaultKey}/minus";
                case KeyCode.Period:
                    return $"{kDefaultKey}/period";
                case KeyCode.Quote:
                    return $"{kDefaultKey}/quote";
                case KeyCode.BackQuote:
                    return $"{kDefaultKey}/backquote";
                case KeyCode.DownArrow:
                    return $"{kDefaultKey}/downArrow";
                case KeyCode.LeftArrow:
                    return $"{kDefaultKey}/leftArrow";
                case KeyCode.RightArrow:
                    return $"{kDefaultKey}/rightArrow";
                case KeyCode.UpArrow:
                    return $"{kDefaultKey}/upArrow";
                case KeyCode.LeftBracket:
                    return $"{kDefaultKey}/leftBracket";
                case KeyCode.RightBracket:
                    return $"{kDefaultKey}/rightBracket";
                case KeyCode.LeftAlt:
                    return $"{kDefaultKey}/leftAlt";
                case KeyCode.RightAlt:
                    return $"{kDefaultKey}/rightAlt";
                case KeyCode.LeftControl:
                    return $"{kDefaultKey}/leftCtrl";
                case KeyCode.RightControl:
                    return $"{kDefaultKey}/rightCtrl";
                case KeyCode.LeftShift:
                    return $"{kDefaultKey}/leftShift";
                case KeyCode.RightShift:
                    return $"{kDefaultKey}/rightShift";
                case KeyCode.CapsLock:
                    return $"{kDefaultKey}/capsLock";
                case KeyCode.Escape:
                    return $"{kDefaultKey}/escape";
                    
            }

            return string.Empty;
        }

        private void CreateNode(Func<AbstractMaterialNode> createNode, Vector2 position)
        {
            AbstractMaterialNode node = createNode();
            DrawState drawState = node.drawState;
            Vector2 posToLocal = GraphView.contentViewContainer.WorldToLocal(position);
            drawState.position = new Rect(posToLocal.x, posToLocal.y, drawState.position.width,
                drawState.position.height);
            node.drawState = drawState;
            GraphData.AddNode(node);
        }
#endif
    }
}