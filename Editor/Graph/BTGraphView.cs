#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SECS.AI.BT.Editor.Graph
{
    public class BTGraphView : GraphView
    {
        public Action RequestSave; 

        private int _idSeed = 1;
        private IEdgeConnectorListener _edgeConnectorListener;

        
        private class NodeSpec
        {
            public string Name;
            public bool IsComposite; 
            public bool HasInput = true;
            public bool HasOutput = false;
            public Port.Capacity OutputCapacity = Port.Capacity.Multi;
            public Port.Capacity InputCapacity = Port.Capacity.Single;
            public bool ShowInPalette = true; 
        }

        private static readonly Dictionary<string, NodeSpec> s_NodeTypes = new()
        {
            ["Selector"] = new NodeSpec{ Name = "Selector", IsComposite = true, HasOutput = true },
            ["Sequence"] = new NodeSpec{ Name = "Sequence", IsComposite = true, HasOutput = true },
            ["Parallel"] = new NodeSpec{ Name = "Parallel", IsComposite = true, HasOutput = true },
            ["Invert"]   = new NodeSpec{ Name = "Invert", IsComposite = true, HasOutput = true, OutputCapacity = Port.Capacity.Single },
            ["Succeeder"] = new NodeSpec{ Name = "Succeeder", IsComposite = true, HasOutput = true, OutputCapacity = Port.Capacity.Single },
            ["Repeater"] = new NodeSpec{ Name = "Repeater", IsComposite = true, HasOutput = true, OutputCapacity = Port.Capacity.Single },
            ["Interrupt"] = new NodeSpec{ Name = "Interrupt", IsComposite = true, HasOutput = true, OutputCapacity = Port.Capacity.Single },
            ["Action"]    = new NodeSpec{ Name = "Action", IsComposite = false, HasOutput = false },
        };

       
        private bool _isDebugMode = false;
        private SelectionDragger _selectionDragger;
        
        public BTGraphView()
        {
            style.flexGrow = 1;
            this.SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            
            
            _selectionDragger = new SelectionDragger();
            this.AddManipulator(_selectionDragger);
            this.AddManipulator(new RectangleSelector());
            
            // 应用样式表
            LoadStyleSheet();
            
            // 创建帮助提示
            CreateHelpHint();
            
            // 注册事件回调
            RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
            RegisterCallback<WheelEvent>(OnWheelEvent);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
            RegisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuPopulate);
            RegisterCallback<KeyDownEvent>(OnKeyDown);

            _edgeConnectorListener = new DefaultEdgeConnectorListener(this);

            var grid = new GridBackground();
            Insert(0, grid);
        }

        private string NewId() => Guid.NewGuid().ToString();

        private Vector2 _lastMouseGraphLocal;
    private Node _ghostNode;
    private string _ghostType;
        private void OnMouseMoveEvent(MouseMoveEvent evt)
        {
           
            _lastMouseGraphLocal = contentViewContainer.WorldToLocal(evt.mousePosition);
        }
        private void OnWheelEvent(WheelEvent evt)
        {
           
            _lastMouseGraphLocal = contentViewContainer.WorldToLocal(evt.mousePosition);
        }
        public void CreateNode(string type)
        {
           
            if (DragAndDrop.GetGenericData("RB_BT_NodeType") is string dragType && dragType == type)
            {
                var e = Event.current;
                if (e != null && !e.alt)
                {
                    return;
                }
            }
            Vector2 pos = _lastMouseGraphLocal;
            if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || layout.width < 1f)
            {
                pos = contentViewContainer.WorldToLocal(new Vector2(this.layout.width/2f, this.layout.height/2f));
            }
            CreateNodeAt(type, pos);
        }
        public void CreateNodeAtLastMouse(string type) => CreateNode(type);

        /// <summary>
        ///右键上下文菜单
        /// </summary>
        private void OnContextualMenuPopulate(ContextualMenuPopulateEvent evt)
        {
            
            if (evt.target is BTGraphView)
            {
                var mousePosition = evt.mousePosition;
                
                // 控制流节点
                evt.menu.AppendSeparator("创建节点/");
                evt.menu.AppendAction("创建节点/控制流/Selector", 
                    (a) => CreateNodeAtPosition("Selector", mousePosition),
                    DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("创建节点/控制流/Sequence", 
                    (a) => CreateNodeAtPosition("Sequence", mousePosition),
                    DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("创建节点/控制流/Parallel", 
                    (a) => CreateNodeAtPosition("Parallel", mousePosition),
                    DropdownMenuAction.AlwaysEnabled);
                
                // 装饰器节点
                evt.menu.AppendSeparator("创建节点/装饰器/");
                evt.menu.AppendAction("创建节点/装饰器/Invert", 
                    (a) => CreateNodeAtPosition("Invert", mousePosition),
                    DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("创建节点/装饰器/Succeeder", 
                    (a) => CreateNodeAtPosition("Succeeder", mousePosition),
                    DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("创建节点/装饰器/Repeater", 
                    (a) => CreateNodeAtPosition("Repeater", mousePosition),
                    DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("创建节点/装饰器/Interrupt", 
                    (a) => CreateNodeAtPosition("Interrupt", mousePosition),
                    DropdownMenuAction.AlwaysEnabled);
                
                // 叶子节点
                evt.menu.AppendSeparator("创建节点/叶子节点/");
                evt.menu.AppendAction("创建节点/叶子节点/Action", 
                    (a) => CreateNodeAtPosition("Action", mousePosition),
                    DropdownMenuAction.AlwaysEnabled);
            }
        }

       
        private void CreateNodeAtPosition(string type, Vector2 screenPosition)
        {
            var graphPosition = contentViewContainer.WorldToLocal(screenPosition);
            CreateNodeAt(type, graphPosition);
        }

       
        private void OnKeyDown(KeyDownEvent evt)
        {
           
            if (panel.focusController.focusedElement != this) return;

           
            bool ctrlPressed = evt.ctrlKey || evt.commandKey;
            
            // 复制粘贴操作
            if (ctrlPressed)
            {
                switch (evt.keyCode)
                {
                    case KeyCode.C:
                        CopySelectedNodes();
                        evt.StopPropagation();
                        return;
                    case KeyCode.V:
                        PasteNodes();
                        evt.StopPropagation();
                        return;
                    case KeyCode.D:
                        DuplicateSelectedNodes();
                        evt.StopPropagation();
                        return;
                }
            }

            // 删除操作
            if (evt.keyCode == KeyCode.Delete)
            {
                DeleteSelectedElements();
                evt.StopPropagation();
                return;
            }

            // 节点创建快捷键
            string nodeType = evt.keyCode switch
            {
                KeyCode.S => "Selector",
                KeyCode.Q => "Sequence", 
                KeyCode.P => "Parallel",
                KeyCode.A => "Action",
                KeyCode.I => "Invert",
                KeyCode.R => "Repeater",
                KeyCode.C when !ctrlPressed => "Condition",
                KeyCode.W => "Wait",
                _ => null
            };

            if (!string.IsNullOrEmpty(nodeType))
            {
               
                var centerPosition = new Vector2(layout.width / 2f, layout.height / 2f);
                var graphPosition = contentViewContainer.WorldToLocal(centerPosition);
                CreateNodeAt(nodeType, graphPosition);
                
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// 为节点应用对应类型的颜色
        /// </summary>
        private void ApplyNodeTypeColor(Node node, string nodeType)
        {
            var color = BTEditorConstants.GetNodeTypeColor(nodeType);
            
          
            var titleContainer = node.titleContainer;
            if (titleContainer != null)
            {
                titleContainer.style.backgroundColor = color;
                
                
                AddNodeTypeIcon(titleContainer, nodeType);
            }

            var cssClass = GetNodeTypeClass(nodeType);
            node.AddToClassList(cssClass);
        }
        
        /// <summary>
        /// 为节点标题添加类型图标
        /// </summary>
    
        private void AddNodeTypeIcon(VisualElement titleContainer, string nodeType)
        {
           
            var existingIcon = titleContainer.Q<Label>("bt-type-icon");
            if (existingIcon != null) return;
            
            var icon = new Label(GetNodeTypeIcon(nodeType))
            {
                name = "bt-type-icon"
            };
            
            icon.style.fontSize = 12;
            icon.style.unityFontStyleAndWeight = FontStyle.Bold;
            icon.style.color = Color.white;
            icon.style.marginRight = 4;
            icon.style.unityTextAlign = TextAnchor.MiddleCenter;
            icon.style.minWidth = 16;
            
            
            titleContainer.Insert(0, icon);
        }
        
        /// <summary>
        /// 根据节点类型获取对应的图标字符
        private string GetNodeTypeIcon(string nodeType)
        {
            return nodeType switch
            {
                // 控制流节点
                "Selector" => "?",     // 选择器 - 表示选择
                "Sequence" => "→",     // 序列器 - 表示顺序
                "Parallel" => "‖",     // 并行器 - 线表示并行
                
                // 装饰器节点
                "Invert" => "!",       // 反转器 - 表示否定
                "Succeeder" => "✓",    // 成功器 - 表示成功
                "Repeater" => "↻",     // 重复器 - 循环箭头
                "Interrupt" => "⏸",    // 中断器 - 暂停符号
                "Cooldown" => "⏱",     // 冷却器 - 时钟符号
                
                // 叶子节点
                "Action" => "⚡",      // 行为节点 - 表示行动
                "Condition" => "?",    // 条件节点 - 表示判断
                "Wait" => "⏳",        // 等待节点 - 表示等待
                
                // 默认
                _ => "●"               // 默认图标 
            };
        }

        /// <summary>
        /// 根据节点类型获取对应的CSS类名
        /// </summary>
        private string GetNodeTypeClass(string nodeType)
        {
            if (BTEditorConstants.IsControlFlowNode(nodeType))
                return "bt-control-flow";
            if (BTEditorConstants.IsDecoratorNode(nodeType))
                return "bt-decorator";
            if (BTEditorConstants.IsLeafNode(nodeType))
                return "bt-leaf";
            
            return "bt-default";
        }

        /// <summary>
        /// 加载样式表
        /// </summary>
        private void LoadStyleSheet()
        {
            try
            {
                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/SimpleECSBehaviorTree/Editor/Graph/BTGraphViewStyles.uss");
                
                if (styleSheet != null)
                {
                    styleSheets.Add(styleSheet);
                }
                else
                {
                    Debug.LogWarning("Could not find BTGraphViewStyles.uss");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load style sheet: {e.Message}");
            }
        }

        /// <summary>
        /// 创建帮助提示UI
        /// </summary>
        private void CreateHelpHint()
        {
            var helpHint = new Label();
            helpHint.AddToClassList("bt-creation-hint");
            helpHint.text = "快捷键:\n" +
                           "S - Selector\n" +
                           "Q - Sequence\n" +
                           "P - Parallel\n" +
                           "A - Action\n" +
                           "I - Invert\n" +
                           "R - Repeater\n" +
                           "C - Condition\n" +
                           "W - Wait\n\n" +
                           "操作:\n" +
                           "右键 - 创建节点菜单\n" +
                           "Ctrl+C - 复制\n" +
                           "Ctrl+V - 粘贴\n" +
                           "Ctrl+D - 复制\n" +
                           "Del - 删除";
            
            helpHint.style.position = Position.Absolute;
            helpHint.style.top = 10;
            helpHint.style.right = 10;
            helpHint.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            helpHint.style.color = new Color(1, 1, 1, 0.8f);
            helpHint.style.paddingTop = 8;
            helpHint.style.paddingBottom = 8;
            helpHint.style.paddingLeft = 12;
            helpHint.style.paddingRight = 12;
            helpHint.style.borderTopLeftRadius = 4;
            helpHint.style.borderTopRightRadius = 4;
            helpHint.style.borderBottomLeftRadius = 4;
            helpHint.style.borderBottomRightRadius = 4;
            helpHint.style.fontSize = 11;
            helpHint.style.maxWidth = 200;
            helpHint.style.whiteSpace = WhiteSpace.Normal;
            
           
            helpHint.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0) 
                {
                    helpHint.style.display = helpHint.style.display == DisplayStyle.None ? 
                                           DisplayStyle.Flex : DisplayStyle.None;
                }
            });

            Add(helpHint);
        }

        
        
        private struct CopiedNodeData
        {
            public string Type;
            public string JsonParams;
            public string DisplayName;
            public string Description;
            public int Order;
            public Vector2 RelativePosition;
        }
        
        private List<CopiedNodeData> _copiedNodes = new List<CopiedNodeData>();
        private Vector2 _copyOffset = Vector2.zero;
        
        /// <summary>
        /// 复制选中的节点
        /// </summary>
        private void CopySelectedNodes()
        {
            _copiedNodes.Clear();
            var selectedNodes = selection.OfType<Node>().ToList();
            
            if (selectedNodes.Count == 0) return;
            
            
            var bounds = GetNodesBounds(selectedNodes);
            _copyOffset = bounds.center;
            
            foreach (var node in selectedNodes)
            {
                if (node.userData is BTNodeMetadata metadata)
                {
                    _copiedNodes.Add(new CopiedNodeData
                    {
                        Type = metadata.Type,
                        JsonParams = metadata.JsonParams,
                        DisplayName = metadata.DisplayName,
                        Description = metadata.Description,
                        Order = metadata.Order,
                        RelativePosition = node.GetPosition().position - _copyOffset
                    });
                }
            }
            
            Debug.Log($"Copied {_copiedNodes.Count} nodes");
        }
        
        /// <summary>
        /// 粘贴节点
        /// </summary>
        private void PasteNodes()
        {
            if (_copiedNodes.Count == 0) return;
            
            ClearSelection();
            
           
            var pastePosition = new Vector2(layout.width / 2f, layout.height / 2f);
            var offset = new Vector2(50, 50); 
            
            foreach (var copiedNode in _copiedNodes)
            {
                var newPosition = pastePosition + copiedNode.RelativePosition + offset;
                var newNode = CreateNodeAt(copiedNode.Type, newPosition);
                
                if (newNode != null && newNode.userData is BTNodeMetadata newMetadata)
                {
                    
                    newMetadata.JsonParams = copiedNode.JsonParams;
                    newMetadata.DisplayName = copiedNode.DisplayName;
                    newMetadata.Description = copiedNode.Description;
                    newMetadata.Order = copiedNode.Order;
                    
                   
                    if (!string.IsNullOrEmpty(copiedNode.DisplayName))
                    {
                        newNode.title = copiedNode.DisplayName;
                    }
                    
                    
                    AddToSelection(newNode);
                }
            }
            
            RequestSave?.Invoke();
            Debug.Log($"Pasted {_copiedNodes.Count} nodes");
        }
        
        /// <summary>
        /// 复制选中的节点
        /// </summary>
        private void DuplicateSelectedNodes()
        {
            CopySelectedNodes();
            PasteNodes();
        }
        
        /// <summary>
        /// 删除选中的元素
        /// </summary>
        private void DeleteSelectedElements()
        {
            if (selection.Count == 0) return;
            
            var elementsToDelete = selection.OfType<GraphElement>().ToList();
            
            // 收集需要删除的边：包括直接选中的边，以及连接到选中节点的所有边
            var edgesToDelete = new HashSet<Edge>();
            
            // 添加直接选中的边
            foreach (var edge in elementsToDelete.OfType<Edge>())
            {
                edgesToDelete.Add(edge);
            }
            
            // 添加连接到选中节点的所有边
            foreach (var node in elementsToDelete.OfType<Node>())
            {
                // 查找所有连接到此节点的边
                foreach (var edge in edges.ToList())
                {
                    if (edge.input?.node == node || edge.output?.node == node)
                    {
                        edgesToDelete.Add(edge);
                    }
                }
            }
            
            // 先删除所有相关的边
            foreach (var edge in edgesToDelete)
            {
                if (!elementsToDelete.Contains(edge))
                {
                    elementsToDelete.Add(edge);
                }
            }
            
            DeleteElements(elementsToDelete);
            RequestSave?.Invoke();
        }
        
        /// <summary>
        /// 获取节点组的边界框
        /// </summary>
        private Rect GetNodesBounds(List<Node> nodes)
        {
            if (nodes.Count == 0) return Rect.zero;
            
            var firstPos = nodes[0].GetPosition();
            var minX = firstPos.xMin;
            var minY = firstPos.yMin;
            var maxX = firstPos.xMax;
            var maxY = firstPos.yMax;
            
            foreach (var node in nodes.Skip(1))
            {
                var pos = node.GetPosition();
                minX = Mathf.Min(minX, pos.xMin);
                minY = Mathf.Min(minY, pos.yMin);
                maxX = Mathf.Max(maxX, pos.xMax);
                maxY = Mathf.Max(maxY, pos.yMax);
            }
            
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 设置调试模式
        /// </summary>
        /// <param name="debugMode">是否为调试模式</param>
        public void SetDebugMode(bool debugMode)
        {
            _isDebugMode = debugMode;
            
            if (debugMode)
            {
               
                DisableNodeInteractions();
                
               
                style.opacity = 0.85f;
                AddToClassList("bt-debug-mode");
            }
            else
            {
               
                EnableNodeInteractions();
                
              
                style.opacity = 1.0f;
                RemoveFromClassList("bt-debug-mode");
            }
        }
        
        /// <summary>
        /// 禁用节点交互
        /// </summary>
        private void DisableNodeInteractions()
        {
           
            if (_selectionDragger != null)
            {
                this.RemoveManipulator(_selectionDragger);
            }
            
            
            foreach (var element in graphElements)
            {
                if (element is UnityEditor.Experimental.GraphView.Node node)
                {
                    node.pickingMode = PickingMode.Ignore;
                }
            }
            
           
            UnregisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuPopulate);
            UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }
        
        /// <summary>
        /// 启用节点交互
        /// </summary>
        private void EnableNodeInteractions()
        {
            
            if (_selectionDragger != null)
            {
                this.AddManipulator(_selectionDragger);
            }
            
           
            foreach (var element in graphElements)
            {
                if (element is UnityEditor.Experimental.GraphView.Node node)
                {
                    node.pickingMode = PickingMode.Position;
                }
            }
             // 恢复右键菜单和快捷键
            RegisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuPopulate);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void EnsureGhost(string type)
        {
            if (_ghostNode != null && _ghostType == type) return;
            ClearGhost();
            if (!s_NodeTypes.TryGetValue(type, out var spec)) return;
            _ghostType = type;
            _ghostNode = new Node { title = spec.Name };
            _ghostNode.style.opacity = 0.4f;
            _ghostNode.pickingMode = PickingMode.Ignore;
            if (spec.HasInput) _ghostNode.inputContainer.Add(MakePort(Direction.Input, spec.InputCapacity));
            if (spec.HasOutput) _ghostNode.outputContainer.Add(MakePort(Direction.Output, spec.OutputCapacity));
            _ghostNode.RefreshExpandedState();
            _ghostNode.RefreshPorts();
            AddElement(_ghostNode);
        }
        private void ClearGhost()
        {
            if (_ghostNode != null)
            {
                RemoveElement(_ghostNode);
                _ghostNode = null; _ghostType = null;
            }
        }
        private void UpdateGhostPosition()
        {
            if (_ghostNode == null) return;
            _ghostNode.SetPosition(new Rect(_lastMouseGraphLocal, new Vector2(BTEditorConstants.DEFAULT_NODE_WIDTH, BTEditorConstants.DEFAULT_NODE_HEIGHT)));
        }
        private void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            if (DragAndDrop.GetGenericData("RB_BT_NodeType") is string type)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                _lastMouseGraphLocal = contentViewContainer.WorldToLocal(evt.mousePosition);
                EnsureGhost(type);
                UpdateGhostPosition();
                evt.StopPropagation();
            }
        }
        private void OnDragPerformEvent(DragPerformEvent evt)
        {
            if (DragAndDrop.GetGenericData("RB_BT_NodeType") is string type)
            {
                DragAndDrop.AcceptDrag();
                _lastMouseGraphLocal = contentViewContainer.WorldToLocal(evt.mousePosition);
                ClearGhost();
                CreateNode(type);
                evt.StopPropagation();
            }
        }
        private void OnDragLeaveEvent(DragLeaveEvent evt)
        {
            ClearGhost();
        }

        public Node CreateNodeAt(string type, Vector2 position)
        {
            if (!s_NodeTypes.TryGetValue(type, out var spec))
            {
                Debug.LogError($"Unknown node type: {type}");
                return null;
            }
            var node = new Node { title = spec.Name };
            node.userData = new BTNodeMetadata 
            { 
                Id = NewId(), 
                Type = spec.Name, 
                JsonParams = "{}", 
                DisplayName = string.Empty, 
                Description = string.Empty, 
                Order = 0 
            };
            node.SetPosition(new Rect(position, new Vector2(BTEditorConstants.DEFAULT_NODE_WIDTH, BTEditorConstants.DEFAULT_NODE_HEIGHT)));

            if (spec.HasInput)
                node.inputContainer.Add(MakePort(Direction.Input, spec.InputCapacity));
            if (spec.HasOutput)
                node.outputContainer.Add(MakePort(Direction.Output, spec.OutputCapacity));

            node.RefreshExpandedState();
            node.RefreshPorts();
            
         
            ApplyNodeTypeColor(node, spec.Name);
            
          
            if (_isDebugMode)
            {
                node.pickingMode = PickingMode.Ignore;
            }
            
          
            BuildNodeInspector(node, spec, new BTGraphData.NodeData{ jsonParams = "{}"});
            AddElement(node);
            RequestSave?.Invoke();
            return node;
        }

        public void Load(BTGraphData data)
        {
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements.ToList());
            _idSeed = 1;

            var id2node = new Dictionary<string, Node>();
            foreach (var n in data.nodes)
            {
                string type = n.type;
                string jsonParams = string.IsNullOrEmpty(n.jsonParams) ? "{}" : n.jsonParams;

                var node = new Node { title = string.IsNullOrEmpty(n.displayName) ? type : n.displayName };
                node.userData = new BTNodeMetadata 
                { 
                    Id = n.id, 
                    Type = type, 
                    JsonParams = jsonParams, 
                    DisplayName = n.displayName, 
                    Description = n.description, 
                    Order = n.order 
                };
                node.SetPosition(new Rect(n.position, new Vector2(BTEditorConstants.DEFAULT_NODE_WIDTH, BTEditorConstants.DEFAULT_NODE_HEIGHT)));
                node.tooltip = n.description;
                if (!s_NodeTypes.TryGetValue(type, out var spec)) spec = new NodeSpec{ Name = type, IsComposite = false };
                if (spec.HasInput)
                    node.inputContainer.Add(MakePort(Direction.Input, spec.InputCapacity));
                if (spec.HasOutput)
                    node.outputContainer.Add(MakePort(Direction.Output, spec.OutputCapacity));
                node.RefreshExpandedState();
                node.RefreshPorts();
                
              
                ApplyNodeTypeColor(node, type);
                
               
                if (_isDebugMode)
                {
                    node.pickingMode = PickingMode.Ignore;
                }
                
              
                BuildNodeInspector(node, spec, new BTGraphData.NodeData{ jsonParams = jsonParams });
                AddElement(node);
                id2node[n.id] = node;
            }

            foreach (var e in data.edges)
            {
                var outNode = id2node[e.outNodeId];
                var inNode = id2node[e.inNodeId];
                Port from = null, to = null;
                if (e.outPortIndex >= 0 && e.outPortIndex < outNode.outputContainer.childCount)
                    from = outNode.outputContainer.ElementAt(e.outPortIndex) as Port;
                if (e.inPortIndex >= 0 && e.inPortIndex < inNode.inputContainer.childCount)
                    to = inNode.inputContainer.ElementAt(e.inPortIndex) as Port;
                if (from != null && to != null)
                {
                    var edgeObj = from.ConnectTo(to);
                    AddElement(edgeObj);
                }
            }

            graphViewChanged += OnGraphViewChanged;
        }

        public BTGraphData Save()
        {
            var data = new BTGraphData();
            foreach (var node in nodes)
            {
                if (node is not Node n) continue;
                if (n.userData is not BTNodeMetadata meta) continue;
                data.nodes.Add(new BTGraphData.NodeData
                {
                    id = meta.Id,
                    type = meta.Type,
                    position = n.GetPosition().position,
                    jsonParams = string.IsNullOrEmpty(meta.JsonParams) ? "{}" : meta.JsonParams,
                    displayName = string.IsNullOrEmpty(meta.DisplayName) ? n.title : meta.DisplayName,
                    description = string.IsNullOrEmpty(meta.Description) ? n.tooltip : meta.Description,
                    order = meta.Order
                });
            }

            foreach (var edge in edges)
            {
                if (edge.output?.node is Node on && edge.input?.node is Node inn)
                {
                    var oMeta = (BTNodeMetadata)on.userData;
                    var iMeta = (BTNodeMetadata)inn.userData;
                   
                    int outPortIndex = on.outputContainer.IndexOf(edge.output);
                    int inPortIndex = inn.inputContainer.IndexOf(edge.input);
                    data.edges.Add(new BTGraphData.EdgeData
                    {
                        outNodeId = oMeta.Id,
                        outPortIndex = outPortIndex,
                        inNodeId = iMeta.Id,
                        inPortIndex = inPortIndex
                    });
                }
            }
            return data;
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            RequestSave?.Invoke();
            return change;
        }

        private void BuildNodeInspector(Node node, NodeSpec spec, BTGraphData.NodeData data)
        {
            var content = new VisualElement();
            content.style.paddingLeft = 6;
            content.style.paddingRight = 6;
            var meta = (BTNodeMetadata)node.userData;
           
            var nameField = new TextField("Name") { value = string.IsNullOrEmpty(meta.DisplayName) ? node.title : meta.DisplayName };
            nameField.RegisterValueChangedCallback(evt => { meta.DisplayName = evt.newValue; node.title = string.IsNullOrEmpty(evt.newValue) ? spec.Name : evt.newValue; RequestSave?.Invoke(); });
            content.Add(nameField);
            var descField = new TextField("Desc") { value = meta.Description, multiline = true };
            descField.RegisterValueChangedCallback(evt => { meta.Description = evt.newValue; node.tooltip = evt.newValue; RequestSave?.Invoke(); });
            content.Add(descField);
          
            var orderField = new IntegerField("Order") { value = meta.Order };
            orderField.tooltip = "父节点下的执行顺序；数值越小越靠前";
            orderField.RegisterValueChangedCallback(v => { meta.Order = v.newValue; RequestSave?.Invoke(); });
            content.Add(orderField);

            if (spec.Name == "Action")
            {
                GenericActionData gad;
                try { gad = string.IsNullOrEmpty(meta.JsonParams)? new GenericActionData(): JsonUtility.FromJson<GenericActionData>(meta.JsonParams); }
                catch { gad = new GenericActionData(); }
                if (gad.parameters == null) gad.parameters = new();

               
                var actionNames = new List<string>(BTActionParamRegistry.GetAllRegisteredActionNames());
                if (!actionNames.Contains(gad.task)) actionNames.Add(gad.task); 
                var taskPopup = new PopupField<string>("Task", actionNames, Math.Max(0, actionNames.IndexOf(gad.task)));
                taskPopup.RegisterValueChangedCallback(evt => { gad.task = evt.newValue; meta.JsonParams = JsonUtility.ToJson(gad); RequestSave?.Invoke(); node.extensionContainer.Clear(); BuildNodeInspector(node, spec, new BTGraphData.NodeData{ jsonParams = meta.JsonParams }); node.RefreshExpandedState(); });
                content.Add(taskPopup);

                foreach (var desc in BTActionParamRegistry.Get(gad.task))
                {
                    string current = gad.Get(desc.name, desc.defaultValue);
                    VisualElement field = null;
                    switch (desc.type)
                    {
                    
                        case BTActionParamType.Float:
                            if (!float.TryParse(current, out var f)) f = float.Parse(desc.defaultValue);
                            var ff = new FloatField(desc.label){ value = f, tooltip = desc.tooltip };
                            ff.RegisterValueChangedCallback(v=> { gad.Set(desc.name, v.newValue.ToString()); meta.JsonParams = JsonUtility.ToJson(gad); RequestSave?.Invoke(); });
                            field = ff; break;
                        case BTActionParamType.Int:
                            if (!int.TryParse(current, out var iv)) iv = int.Parse(desc.defaultValue);
                            var inf = new IntegerField(desc.label){ value = iv, tooltip = desc.tooltip };
                            inf.RegisterValueChangedCallback(v=> { gad.Set(desc.name, v.newValue.ToString()); meta.JsonParams = JsonUtility.ToJson(gad); RequestSave?.Invoke(); });
                            field = inf; break;
                        case BTActionParamType.Bool:
                            bool bv = current == "True" || current == "true" || current == "1";
                            var tog = new Toggle(desc.label){ value = bv, tooltip = desc.tooltip };
                            tog.RegisterValueChangedCallback(v=> { gad.Set(desc.name, v.newValue ? "true":"false"); meta.JsonParams = JsonUtility.ToJson(gad); RequestSave?.Invoke(); });
                            field = tog; break;
                        case BTActionParamType.String:
                            var tf = new TextField(desc.label){ value = current, tooltip = desc.tooltip };
                            tf.RegisterValueChangedCallback(v=> { gad.Set(desc.name, v.newValue); meta.JsonParams = JsonUtility.ToJson(gad); RequestSave?.Invoke(); });
                            field = tf; break;
                        // case BTActionParamType.Enum:
                        //     var opts = desc.name == "filter" ? new List<string>{ "Any", "EnemyTower", "EnemyUnit" } : new List<string>{ current };
                        //     if (!opts.Contains(current)) opts.Add(current);
                        //     var pf = new PopupField<string>(desc.label, opts, Math.Max(0, opts.IndexOf(current))) { tooltip = desc.tooltip };
                        //     pf.RegisterValueChangedCallback(v=> { gad.Set(desc.name, v.newValue); meta.JsonParams = JsonUtility.ToJson(gad); RequestSave?.Invoke(); });
                        //     field = pf; break;
                    }
                    if (field != null) content.Add(field);
                }
                meta.JsonParams = JsonUtility.ToJson(gad);
            }
            else if (spec.Name == "Repeater")
            {
                var rp = string.IsNullOrEmpty(meta.JsonParams) ? new RepeaterBakeParams() : JsonUtility.FromJson<RepeaterBakeParams>(meta.JsonParams);
                var countField = new IntegerField("Repeat Count") { value = rp.count };
                countField.tooltip = "执行次数1 = 无限循环";
                countField.RegisterValueChangedCallback(v => { rp.count = v.newValue; meta.JsonParams = JsonUtility.ToJson(rp); RequestSave?.Invoke(); });
                content.Add(countField);
                meta.JsonParams = JsonUtility.ToJson(rp);
            }
            else if (spec.Name == "Interrupt")
            {
                var ip = string.IsNullOrEmpty(meta.JsonParams) ? new InterruptBakeParams() : JsonUtility.FromJson<InterruptBakeParams>(meta.JsonParams);
                var keyField = new TextField("Key") { value = ip.key };
                keyField.tooltip = "黑板键名";
                keyField.RegisterValueChangedCallback(v => { ip.key = v.newValue; meta.JsonParams = JsonUtility.ToJson(ip); RequestSave?.Invoke(); });
                content.Add(keyField);
                meta.JsonParams = JsonUtility.ToJson(ip);
            }

            if (content.childCount > 0)
            {
                node.extensionContainer.Add(content);
                node.RefreshExpandedState();
            }
        }

        private Port MakePort(Direction direction, Port.Capacity capacity)
        {
            var port = Port.Create<Edge>(Orientation.Horizontal, direction, capacity, typeof(float));
            port.portName = direction == Direction.Input ? "In" : "Out";
            port.AddManipulator(new EdgeConnector<Edge>(_edgeConnectorListener));
            return port;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatible = new List<Port>();
            ports.ForEach((port) =>
            {
                if (startPort == port) return;
                if (startPort.node == port.node) return;
                if (startPort.direction == port.direction) return;
               
                bool StartIsOutput = startPort.direction == Direction.Output;
                var startMeta = (BTNodeMetadata)startPort.node.userData;
                var portMeta = (BTNodeMetadata)port.node.userData;
                var startSpecOk = s_NodeTypes.TryGetValue(startMeta.Type, out var startSpec);
                var portSpecOk   = s_NodeTypes.TryGetValue(portMeta.Type, out var portSpec);
                if (!startSpecOk || !portSpecOk) return;

                if (StartIsOutput)
                {
                    if (!startSpec.IsComposite) return; 
                  
                    if (port.direction != Direction.Input) return;
                }
                else
                {
                    
                    if (port.direction != Direction.Output) return;
                    if (!portSpec.IsComposite) return;
                }

                compatible.Add(port);
            });
            return compatible;
        }

        private class DefaultEdgeConnectorListener : IEdgeConnectorListener
        {
            private readonly GraphView _graphView;
            public DefaultEdgeConnectorListener(GraphView graphView)
            {
                _graphView = graphView;
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
                
                var startPort = edge.output ?? edge.input; 
                if (startPort == null) return;

                
                var menu = new GenericMenu();
                bool hasItems = false;
                foreach (var kv in s_NodeTypes)
                {
                    var spec = kv.Value;
                    if (edge.input == null && edge.output != null)
                    {
                       
                        hasItems = true;
                        menu.AddItem(new GUIContent(spec.Name), false, () =>
                        {
                            var world = position;
                            var local = ((BTGraphView)_graphView).contentViewContainer.WorldToLocal(world);
                            var newNode = ((BTGraphView)_graphView).CreateNodeAt(spec.Name, local);
                            if (newNode == null) return;
                            var targetInput = newNode.inputContainer.Q<Port>();
                            if (targetInput == null) return;
                            
                            RemoveSingleCapacityConnections(startPort);
                            RemoveSingleCapacityConnections(targetInput);
                            var newEdge = startPort.ConnectTo(targetInput);
                            _graphView.AddElement(newEdge);
                        });
                    }
                    else if (edge.input != null && edge.output == null)
                    {
                        
                        if (!spec.IsComposite) { continue; }
                        hasItems = true;
                        menu.AddItem(new GUIContent(spec.Name), false, () =>
                        {
                            var world = position;
                            var local = ((BTGraphView)_graphView).contentViewContainer.WorldToLocal(world);
                            var newNode = ((BTGraphView)_graphView).CreateNodeAt(spec.Name, local);
                            if (newNode == null) return;
                            var sourceOutput = newNode.outputContainer.Q<Port>();
                            if (sourceOutput == null) return;
                          
                            RemoveSingleCapacityConnections(sourceOutput);
                            RemoveSingleCapacityConnections(startPort);
                            var newEdge = sourceOutput.ConnectTo(startPort);
                            _graphView.AddElement(newEdge);
                        });
                    }
                }
                if (!hasItems)
                {
                    
                    return;
                }
                menu.ShowAsContext();
            }

            private void RemoveSingleCapacityConnections(Port port)
            {
                if (port == null) return;
                if (port.capacity != Port.Capacity.Single) return;
                foreach (var c in port.connections.ToList())
                {
                    _graphView.RemoveElement(c);
                }
            }

            public void OnDrop(GraphView graphView, Edge edge)
            {
                if (edge.input == null || edge.output == null)
                    return;

              
                if (edge.input.capacity == Port.Capacity.Single)
                {
                    foreach (var c in edge.input.connections.ToList())
                    {
                        _graphView.RemoveElement(c);
                    }
                }

                if (edge.output.capacity == Port.Capacity.Single)
                {
                    foreach (var c in edge.output.connections.ToList())
                    {
                        _graphView.RemoveElement(c);
                    }
                }

                _graphView.AddElement(edge);
            }
        }
    }
}
#endif
