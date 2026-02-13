using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dialogs.Editor
{
    public class DialogEditorv2 : EditorWindow
    {
        Dialog selectedDialog = null;

        // Canvas principal donde se dibujan los nodos
        VisualElement canvas;
        // Contenedor que permite scroll y zoom
        ScrollView scrollView;

        // Estado de arrastre y linking
        [NonSerialized] DialogNode draggingNode = null;
        [NonSerialized] Vector2 draggingOffset;
        [NonSerialized] DialogNode linkingParentNode = null;

        // Mapeo nodo → elemento visual para actualizar rápidamente
        readonly Dictionary<string, VisualElement> nodeElements = new();
        // Conexiones IMGUIContainer para las líneas bezier
        IMGUIContainer connectionsLayer;

        // Zoom
        float zoom = 1f;
        const float MinZoom = 0.4f;
        const float MaxZoom = 2.5f;
        const float ZoomStep = 0.1f;
        const float canvasSize = 4000f;

        [MenuItem("Window/Dialogue EditorV2")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(DialogEditor), false, "Dialogue EditorV2");
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            Dialog dialog = EditorUtility.InstanceIDToObject(instanceID) as Dialog;
            if (dialog != null)
            {
                ShowEditorWindow();
                return true;
            }
            return false;
        }

        [Obsolete]
        public void CreateGUI()
        {
            // --- Hoja de estilos ---
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/scripts/Personajes/Dialogues/Editor/DialogEditor.uss");

            rootVisualElement.styleSheets.Add(styleSheet);
            rootVisualElement.AddToClassList("root");

            // --- Toolbar superior ---
            var toolbar = new VisualElement();
            toolbar.AddToClassList("toolbar");

            var zoomLabel = new Label("Zoom: 100%");
            zoomLabel.name = "zoom-label";
            toolbar.Add(zoomLabel);

            var zoomInBtn = new Button(() => ApplyZoom(ZoomStep, zoomLabel)) { text = "+" };
            zoomInBtn.AddToClassList("toolbar-button");
            toolbar.Add(zoomInBtn);

            var zoomOutBtn = new Button(() => ApplyZoom(-ZoomStep, zoomLabel)) { text = "-" };
            zoomOutBtn.AddToClassList("toolbar-button");
            toolbar.Add(zoomOutBtn);

            rootVisualElement.Add(toolbar);

            // --- ScrollView que contiene el canvas ---
            scrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            scrollView.AddToClassList("graph-scroll");
            rootVisualElement.Add(scrollView);

            // --- Canvas (contenedor de nodos) ---
            canvas = new VisualElement();
            canvas.name = "canvas";
            canvas.AddToClassList("canvas");
            canvas.style.width = canvasSize;
            canvas.style.height = canvasSize;

            // Fondo con textura
            var backgroundTex = Resources.Load<Texture2D>("background");
            if (backgroundTex != null)
            {
                canvas.style.backgroundImage = backgroundTex;
                canvas.style.unityBackgroundImageTintColor = new Color(1, 1, 1, 0.3f);
            }

            // --- Capa IMGUI para dibujar conexiones bezier ---
            connectionsLayer = new IMGUIContainer(DrawAllConnections);
            connectionsLayer.name = "connections-layer";
            connectionsLayer.style.position = Position.Absolute;
            connectionsLayer.style.left = 0;
            connectionsLayer.style.top = 0;
            connectionsLayer.style.width = canvasSize;
            connectionsLayer.style.height = canvasSize;
            connectionsLayer.pickingMode = PickingMode.Ignore;
            canvas.Add(connectionsLayer);

            scrollView.Add(canvas);

            // --- Zoom con Ctrl + Scroll ---
            scrollView.RegisterCallback<WheelEvent>(evt =>
            {
                if (evt.ctrlKey || evt.commandKey)
                {
                    float delta = -evt.delta.y;
                    ApplyZoom(delta * ZoomStep * 0.2f, zoomLabel);
                    evt.StopPropagation();
                    evt.PreventDefault();
                }
            });

            // --- Clic en vacío selecciona el diálogo ---
            canvas.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0 && evt.target == canvas)
                {
                    Selection.activeObject = selectedDialog;
                }
            });

            // Escuchar cambio de selección
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }

        private void OnDestroy()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            Dialog newDialog = Selection.activeObject as Dialog;
            if (newDialog != null && newDialog != selectedDialog)
            {
                selectedDialog = newDialog;
                RebuildGraph();
            }
        }

        // ============================================================
        //  RECONSTRUIR GRAFO COMPLETO
        // ============================================================
        void RebuildGraph()
        {
            // Limpiar nodos anteriores (mantener la capa de conexiones)
            var toRemove = canvas.Children()
                .Where(c => c != connectionsLayer)
                .ToList();
            foreach (var c in toRemove)
                canvas.Remove(c);

            nodeElements.Clear();
            linkingParentNode = null;

            if (selectedDialog == null) return;

            foreach (DialogNode node in selectedDialog.GetAllNodes())
            {
                var nodeEl = CreateNodeElement(node);
                canvas.Add(nodeEl);
                nodeElements[node.GetID()] = nodeEl;
            }

            connectionsLayer.MarkDirtyRepaint();
        }

        // ============================================================
        //  CREAR ELEMENTO VISUAL DE UN NODO
        // ============================================================
        VisualElement CreateNodeElement(DialogNode node)
        {
            Rect r = node.GetRect();

            var container = new VisualElement();
            container.name = "node-" + node.GetID();
            container.AddToClassList("node");
            container.AddToClassList(node.IsPlayerSpeaking() ? "node-player" : "node-npc");
            container.style.position = Position.Absolute;
            container.style.left = r.x * zoom;
            container.style.top = r.y * zoom;
            container.style.width = r.width * zoom;
            container.style.minHeight = r.height * zoom;

            // --- ID Label ---
            var idLabel = new Label("ID: " + node.name);
            idLabel.AddToClassList("node-id");
            container.Add(idLabel);

            // --- Separador ---
            var separator = new VisualElement();
            separator.AddToClassList("node-separator");
            container.Add(separator);

            // --- Etiqueta "Dialogue:" ---
            var dialogueLabel = new Label("Dialogue:");
            dialogueLabel.AddToClassList("node-dialogue-label");
            container.Add(dialogueLabel);

            // --- TextField para el texto del diálogo ---
            var textField = new TextField { multiline = true, value = node.GetText() ?? "" };
            textField.AddToClassList("node-text");
            textField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(node, "Edit Dialogue Text");
                node.SetText(evt.newValue);
                EditorUtility.SetDirty(node);
            });
            container.Add(textField);

            // --- Barra de botones ---
            var buttonBar = new VisualElement();
            buttonBar.AddToClassList("node-button-bar");

            // Botón eliminar
            var deleteBtn = new Button(() =>
            {
                if (selectedDialog.GetAllNodesCount() <= 1)
                    EditorUtility.DisplayDialog("Delete Node",
                        "Cannot delete the last node in the dialogue.", "OK");
                else
                {
                    selectedDialog.DeleteNode(node);
                    RebuildGraph();
                }
            })
            { text = "✕" };
            deleteBtn.AddToClassList("btn-delete");
            buttonBar.Add(deleteBtn);

            // Botón link
            var linkBtn = new Button() { text = "Link" };
            linkBtn.AddToClassList("btn-link");
            linkBtn.clicked += () => OnLinkClicked(node, linkBtn);
            buttonBar.Add(linkBtn);

            // Botón añadir hijo
            var addBtn = new Button(() =>
            {
                selectedDialog.CreateNode(node);
                RebuildGraph();
            })
            { text = "+" };
            addBtn.AddToClassList("btn-add");
            buttonBar.Add(addBtn);

            container.Add(buttonBar);

            // --- Toggle Player Speaking ---
            var speakerToggle = new Toggle("Player Speaking") { value = node.IsPlayerSpeaking() };
            speakerToggle.AddToClassList("node-speaker-toggle");
            speakerToggle.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(node, "Toggle Speaker");
                node.SetPlayerSpeaking(evt.newValue);
                EditorUtility.SetDirty(node);

                // Actualizar clase visual
                container.RemoveFromClassList("node-player");
                container.RemoveFromClassList("node-npc");
                container.AddToClassList(evt.newValue ? "node-player" : "node-npc");
            });
            container.Add(speakerToggle);

            // --- Arrastre del nodo ---
            container.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    draggingNode = node;
                    Vector2 nodePos = new(container.resolvedStyle.left, container.resolvedStyle.top);
                    draggingOffset = nodePos - evt.mousePosition;
                    Selection.activeObject = node;
                    container.CaptureMouse();
                    evt.StopPropagation();
                }
            });

            container.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (draggingNode == node && container.HasMouseCapture())
                {
                    Vector2 newPos = (evt.mousePosition + draggingOffset) / zoom;
                    node.SetPosition(newPos);

                    container.style.left = newPos.x * zoom;
                    container.style.top = newPos.y * zoom;

                    connectionsLayer.MarkDirtyRepaint();
                    evt.StopPropagation();
                }
            });

            container.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (draggingNode == node)
                {
                    draggingNode = null;
                    container.ReleaseMouse();
                    evt.StopPropagation();
                }
            });

            return container;
        }

        // ============================================================
        //  LÓGICA DE LINKING
        // ============================================================
        void OnLinkClicked(DialogNode node, Button linkBtn)
        {
            if (linkingParentNode == null)
            {
                // Iniciar linking
                linkingParentNode = node;
                linkBtn.text = "Cancel";
                linkBtn.AddToClassList("btn-cancel");
            }
            else if (linkingParentNode == node)
            {
                // Cancelar
                linkingParentNode = null;
                linkBtn.text = "Link";
                linkBtn.RemoveFromClassList("btn-cancel");
            }
            else if (linkingParentNode.GetChildren().Contains(node.GetID()))
            {
                // Desenlazar
                linkingParentNode.RemoveChild(node.GetID());
                linkingParentNode = null;
                RebuildGraph();
            }
            else
            {
                // Enlazar como hijo
                Undo.RecordObject(selectedDialog, "Link Dialog Nodes");
                linkingParentNode.AddChild(node.GetID());
                linkingParentNode = null;
                RebuildGraph();
            }
        }

        // ============================================================
        //  DIBUJAR CONEXIONES (IMGUI dentro de UIElements)
        // ============================================================
        void DrawAllConnections()
        {
            if (selectedDialog == null) return;

            foreach (DialogNode node in selectedDialog.GetAllNodes())
            {
                Rect r = node.GetRect();
                Vector2 start = new(r.xMax * zoom - 7 * zoom, r.center.y * zoom);

                foreach (var child in selectedDialog.GetAllChildren(node))
                {
                    Rect cr = child.GetRect();
                    Vector2 end = new(cr.xMin * zoom + 7 * zoom, cr.center.y * zoom);
                    Vector2 cp = end - start;
                    cp.y = 0;
                    cp.x *= 0.8f;
                    Handles.DrawBezier(start, end, start + cp, end - cp, Color.white, null, 4f);
                }
            }
        }

        // ============================================================
        //  ZOOM
        // ============================================================
        void ApplyZoom(float delta, Label zoomLabel)
        {
            float target = Mathf.Clamp(zoom + delta, MinZoom, MaxZoom);
            if (Mathf.Approximately(target, zoom)) return;
            zoom = target;

            zoomLabel.text = $"Zoom: {Mathf.RoundToInt(zoom * 100)}%";

            // Reposicionar y redimensionar todos los nodos
            if (selectedDialog == null) return;
            foreach (DialogNode node in selectedDialog.GetAllNodes())
            {
                if (nodeElements.TryGetValue(node.GetID(), out var el))
                {
                    Rect r = node.GetRect();
                    el.style.left = r.x * zoom;
                    el.style.top = r.y * zoom;
                    el.style.width = r.width * zoom;
                    el.style.minHeight = r.height * zoom;
                }
            }
            connectionsLayer.MarkDirtyRepaint();
        }
    }
}
