using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NKStudio.ShaderGraph.HotKey
{
    public class NodeInputAssetObserver : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths, bool didDomainReload)
        {
            int actionId = EditorPrefs.GetInt("SGHKInputActionID", -1);

            //기존에 있던 것을 반영합니다.
            string path = AssetDatabase.GetAssetPath(actionId);

            //없으면 리턴
            if (path.Equals(string.Empty)) return;

            //있으면 변경될 때 CallBack
            if (importedAssets.Contains(path))
            {
                InputActionAsset inputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);

                //키를 가지고 있는 녀석을 모두 찾습니다.
                Dictionary<string, string> hasHotKeyNodes = new Dictionary<string, string>();
                foreach (InputActionMap actionMap in inputActionAsset.actionMaps)
                foreach (InputAction action in actionMap.actions)
                foreach (InputBinding binding in action.bindings.Where(binding => binding.path.Length != 0))
                    hasHotKeyNodes.Add(action.name, binding.path);

                //중복된 노드들을 수록합니다.
                var duplicateNodes = new Dictionary<string, int>();

                //단축키를 가지고 있는 노드들 중에서,
                foreach (var node in hasHotKeyNodes)
                {
                    //해당 단축키가 추가되어있는지 체크
                    if (duplicateNodes.ContainsKey(node.Value))
                    {
                        //있으면 해당 단축키로 찾아서 Count에 1 더함
                        duplicateNodes[node.Value] += 1;
                    }
                    else
                        duplicateNodes.Add(node.Value, 1);
                }

                //키 바인딩이 중복되면 해당 키를 카운팅함.
                foreach (var duplicateNode in duplicateNodes)
                {
                    //해당 단축키가 2개 이상 바인딩 되어 있다면, 다이어로그를 띄워서 방지한다.
                    if (duplicateNode.Value >= 2)
                    {
                        List<string> dNode = new();
                        //Hot Key를 모두 순례를 돈다.
                        foreach (var hotKeyNode in hasHotKeyNodes)
                        {
                            //hotKeyNode(단축키) == duplicateNode(단축키)
                            if (hotKeyNode.Value == duplicateNode.Key)
                            {
                                //이름 표시
                                dNode.Add(hotKeyNode.Key);
                            }
                        }

                        bool msg = EditorUtility.DisplayDialog("단축키 중복",
                            $"{dNode[0]}과 {dNode[1]}의 단축키가 중복됩니다.\n 다음 중 어떤 노드의 단축키를 삭제하겠습니까?", dNode[1], dNode[0]);

                        //값을 비웁니다.
                        ChangeBinding(inputActionAsset, msg ? dNode[1] : dNode[0], "");

                        //저장
                        string jData = inputActionAsset.ToJson();

                        //데이터를 저장합니다.
                        File.WriteAllText(path, jData);

                        //새로고침
                        AssetDatabase.Refresh();
                        
                        //변경사항 있음
                        if (msg)
                        {
                            Debug.Log("수정 되었습니다.");
                            RemoveHotKeyDefine();
                            EditorPrefs.SetBool("UpdateHint", true);
                            //EditorApplication.ExecuteMenuItem("Window/ShaderGraph HotKey/ShaderGraphSettings");
                        }

                        return;
                    }
                }

                RemoveHotKeyDefine();
                EditorPrefs.SetBool("UpdateHint", true);
                //EditorApplication.ExecuteMenuItem("Window/ShaderGraph HotKey/ShaderGraphSettings");
            }
        }

        private static void RemoveHotKeyDefine()
        {
            const string hotKeyDefine = "SHADER_GRAPH_HOTKEY";

            //현재 플레이어 세팅스의 디파인을 가져옵니다.
            string defines =
                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            string removed = defines.Replace(hotKeyDefine, "");

            //프로젝트 세팅스에서 디파인 제거
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                removed);
        }

        private static void ChangeBinding(InputActionAsset inputActionAsset, string actionName, string path)
        {
            if (inputActionAsset.FindAction(actionName).bindings.Count == 0)
            {
                inputActionAsset.FindAction(actionName).AddBinding(path, groups: "Keyboard");
            }
            else
                inputActionAsset.FindAction(actionName).ChangeBinding(0).WithPath(path);
        }
    }
}