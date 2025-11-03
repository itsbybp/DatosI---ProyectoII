using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

namespace WorldMapZoom
{
    public partial class FamilyTreeForm : Form
    {
        private FamilyTree _familyTree;
        private Panel _scrollPanel;
        private PictureBox _drawBox;
        private Dictionary<string, PointF> _nodePositions;
        private Dictionary<string, int> _personLevels;
        private const int NODE_WIDTH = 200;
        private const int NODE_HEIGHT = 110;
        private const int H_SPACING = 80;
        private const int V_SPACING = 180;
        private const int MARGIN = 50;
        private const int COUPLE_SPACING = 40;

        public FamilyTreeForm(FamilyTree familyTree)
        {
            _familyTree = familyTree;
            _nodePositions = new Dictionary<string, PointF>();
            _personLevels = new Dictionary<string, int>();
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = "Árbol Genealógico";
            Width = 1600;
            Height = 900;
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            BackColor = Color.FromArgb(245, 245, 245);

            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(0, 122, 204)
            };
            Controls.Add(headerPanel);

            var titleLabel = new Label
            {
                Text = "🌳 ÁRBOL GENEALÓGICO FAMILIAR",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            headerPanel.Controls.Add(titleLabel);

            _scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };
            Controls.Add(_scrollPanel);

            _drawBox = new PictureBox
            {
                BackColor = Color.White,
                Location = new Point(0, 0)
            };
            _drawBox.Paint += DrawBox_Paint;
            _scrollPanel.Controls.Add(_drawBox);

            CalculatePositions();
        }

        private void CalculatePositions()
        {
            _nodePositions.Clear();
            _personLevels.Clear();
            
            var roots = _familyTree.GetRootMembers().OrderBy(p => p.BirthDate).ToList();
            
            if (roots.Count == 0)
            {
                _drawBox.Size = new Size(1000, 500);
                return;
            }

            // PASO 1: Asignar niveles generacionales
            foreach (var root in roots)
            {
                AssignLevels(root, 0, new HashSet<string>());
            }

            // PASO 2: Organizar por niveles
            var levels = new Dictionary<int, List<Person>>();
            foreach (var person in _familyTree.GetAllMembers())
            {
                if (_personLevels.ContainsKey(person.Id))
                {
                    int level = _personLevels[person.Id];
                    if (!levels.ContainsKey(level))
                        levels[level] = new List<Person>();
                    levels[level].Add(person);
                }
            }

            int maxLevel = levels.Keys.Count > 0 ? levels.Keys.Max() : 0;

            // PASO 3: Posicionar de abajo hacia arriba para mejor distribución
            for (int level = maxLevel; level >= 0; level--)
            {
                if (!levels.ContainsKey(level)) continue;
                
                if (level == maxLevel)
                {
                    // Última generación: posición simple de izquierda a derecha
                    PositionBottomLevel(level, levels[level]);
                }
                else
                {
                    // Generaciones superiores: centrar sobre hijos
                    PositionParentLevel(level, levels[level]);
                }
            }

            // PASO 4: Eliminar todas las superposiciones
            for (int iteration = 0; iteration < 3; iteration++)
            {
                for (int level = 0; level <= maxLevel; level++)
                {
                    if (!levels.ContainsKey(level)) continue;
                    FixOverlapsInLevel(level, levels[level]);
                }
            }

            // Ajustar canvas
            if (_nodePositions.Count > 0)
            {
                float maxX = _nodePositions.Values.Max(p => p.X) + NODE_WIDTH + MARGIN;
                float maxY = _nodePositions.Values.Max(p => p.Y) + NODE_HEIGHT + MARGIN;
                _drawBox.Size = new Size((int)maxX, (int)maxY);
            }

            _drawBox.Invalidate();
        }

        private void AssignLevels(Person person, int level, HashSet<string> visited)
        {
            if (visited.Contains(person.Id))
                return;

            visited.Add(person.Id);

            if (!_personLevels.ContainsKey(person.Id))
                _personLevels[person.Id] = level;
            else
                _personLevels[person.Id] = Math.Min(_personLevels[person.Id], level);

            // Asignar mismo nivel a cónyuges
            var spouses = _familyTree.GetSpouses(person.Id);
            foreach (var spouse in spouses)
            {
                if (!_personLevels.ContainsKey(spouse.Id))
                    _personLevels[spouse.Id] = level;
            }

            // Hijos van al siguiente nivel
            var children = _familyTree.GetChildren(person.Id);
            foreach (var child in children)
            {
                AssignLevels(child, level + 1, visited);
            }
        }

        private void PositionBottomLevel(int level, List<Person> persons)
        {
            float y = MARGIN + (level * V_SPACING);
            float x = MARGIN;
            var processed = new HashSet<string>();

            var sorted = persons.OrderBy(p => p.BirthDate).ToList();

            foreach (var person in sorted)
            {
                if (processed.Contains(person.Id)) continue;

                var spouses = _familyTree.GetSpouses(person.Id)
                    .Where(s => _personLevels.ContainsKey(s.Id) && _personLevels[s.Id] == level && !processed.Contains(s.Id))
                    .ToList();

                if (spouses.Count > 0)
                {
                    var spouse = spouses[0];
                    _nodePositions[person.Id] = new PointF(x, y);
                    _nodePositions[spouse.Id] = new PointF(x + NODE_WIDTH + COUPLE_SPACING, y);
                    processed.Add(person.Id);
                    processed.Add(spouse.Id);
                    x += NODE_WIDTH * 2 + COUPLE_SPACING + H_SPACING;
                }
                else
                {
                    _nodePositions[person.Id] = new PointF(x, y);
                    processed.Add(person.Id);
                    x += NODE_WIDTH + H_SPACING;
                }
            }
        }

        private void PositionParentLevel(int level, List<Person> persons)
        {
            float y = MARGIN + (level * V_SPACING);
            var processed = new HashSet<string>();

            // Agrupar padres con sus hijos
            var parentGroups = new List<ParentGroup>();

            foreach (var person in persons)
            {
                if (processed.Contains(person.Id)) continue;

                var children = _familyTree.GetChildren(person.Id)
                    .Where(c => _personLevels.ContainsKey(c.Id) && 
                               _personLevels[c.Id] == level + 1 &&
                               _nodePositions.ContainsKey(c.Id))
                    .ToList();

                if (children.Count == 0) continue;

                var spouse = _familyTree.GetSpouses(person.Id)
                    .FirstOrDefault(s => _personLevels.ContainsKey(s.Id) && _personLevels[s.Id] == level);

                var group = new ParentGroup
                {
                    Parent1 = person,
                    Parent2 = spouse,
                    Children = children
                };

                parentGroups.Add(group);
                processed.Add(person.Id);
                if (spouse != null) processed.Add(spouse.Id);
            }

            // Ordenar grupos por posición de hijos
            parentGroups = parentGroups.OrderBy(g => g.Children.Min(c => _nodePositions[c.Id].X)).ToList();

            // Posicionar cada grupo de padres
            foreach (var group in parentGroups)
            {
                var childPositions = group.Children.Select(c => _nodePositions[c.Id].X).OrderBy(x => x).ToList();
                float leftChildX = childPositions.First();
                float rightChildX = childPositions.Last();
                float centerX = (leftChildX + rightChildX) / 2;

                if (group.Parent2 != null)
                {
                    // Pareja: centrar sobre hijos
                    float totalWidth = NODE_WIDTH * 2 + COUPLE_SPACING;
                    float startX = centerX - totalWidth / 2;
                    _nodePositions[group.Parent1.Id] = new PointF(startX, y);
                    _nodePositions[group.Parent2.Id] = new PointF(startX + NODE_WIDTH + COUPLE_SPACING, y);
                }
                else
                {
                    // Padre/madre soltero: centrar directamente
                    _nodePositions[group.Parent1.Id] = new PointF(centerX - NODE_WIDTH / 2, y);
                }
            }

            // Posicionar personas sin hijos
            float x = MARGIN;
            if (parentGroups.Count > 0)
            {
                x = _nodePositions.Values.Where(p => Math.Abs(p.Y - y) < 1).Max(p => p.X) + NODE_WIDTH + H_SPACING;
            }

            foreach (var person in persons)
            {
                if (processed.Contains(person.Id)) continue;

                var spouse = _familyTree.GetSpouses(person.Id)
                    .FirstOrDefault(s => _personLevels.ContainsKey(s.Id) && 
                                       _personLevels[s.Id] == level && 
                                       !processed.Contains(s.Id));

                if (spouse != null)
                {
                    _nodePositions[person.Id] = new PointF(x, y);
                    _nodePositions[spouse.Id] = new PointF(x + NODE_WIDTH + COUPLE_SPACING, y);
                    processed.Add(person.Id);
                    processed.Add(spouse.Id);
                    x += NODE_WIDTH * 2 + COUPLE_SPACING + H_SPACING;
                }
                else
                {
                    _nodePositions[person.Id] = new PointF(x, y);
                    processed.Add(person.Id);
                    x += NODE_WIDTH + H_SPACING;
                }
            }
        }

        private void FixOverlapsInLevel(int level, List<Person> persons)
        {
            var nodesInLevel = persons
                .Where(p => _nodePositions.ContainsKey(p.Id))
                .Select(p => new { Person = p, Pos = _nodePositions[p.Id] })
                .Where(x => Math.Abs(x.Pos.Y - (MARGIN + level * V_SPACING)) < 10)
                .OrderBy(x => x.Pos.X)
                .ToList();

            for (int i = 1; i < nodesInLevel.Count; i++)
            {
                var prev = nodesInLevel[i - 1];
                var curr = nodesInLevel[i];

                float minDistance = NODE_WIDTH + H_SPACING;
                float currentDistance = curr.Pos.X - prev.Pos.X;

                if (currentDistance < minDistance)
                {
                    float shift = minDistance - currentDistance;
                    var newPos = new PointF(curr.Pos.X + shift, curr.Pos.Y);
                    _nodePositions[curr.Person.Id] = newPos;
                    
                    // Actualizar nodos siguientes
                    for (int j = i + 1; j < nodesInLevel.Count; j++)
                    {
                        var next = nodesInLevel[j];
                        var nextPos = _nodePositions[next.Person.Id];
                        _nodePositions[next.Person.Id] = new PointF(nextPos.X + shift, nextPos.Y);
                    }
                }
            }
        }

        private class ParentGroup
        {
            public Person Parent1 { get; set; }
            public Person Parent2 { get; set; }
            public List<Person> Children { get; set; }
        }

        private void DrawBox_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            if (_nodePositions.Count == 0)
            {
                DrawEmptyMessage(g);
                return;
            }

            // Primero todas las líneas
            DrawAllLines(g);

            // Después todos los nodos
            foreach (var kvp in _nodePositions)
            {
                var person = _familyTree.GetPerson(kvp.Key);
                if (person != null)
                {
                    DrawNode(g, person, kvp.Value);
                }
            }
        }

        private void DrawEmptyMessage(Graphics g)
        {
            string msg = "No hay miembros en el árbol.\n\nAgregue personas usando el botón 'Agregar Persona'.";
            var font = new Font("Segoe UI", 18);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(msg, font, Brushes.Gray, _drawBox.Width / 2, _drawBox.Height / 2, sf);
        }

        private void DrawAllLines(Graphics g)
        {
            var drawnMarriages = new HashSet<string>();
            var drawnParentChild = new HashSet<string>();

            // 1. Líneas de matrimonio (horizontales)
            foreach (var person in _familyTree.GetAllMembers())
            {
                if (!_nodePositions.ContainsKey(person.Id)) continue;

                var spouses = _familyTree.GetSpouses(person.Id);
                foreach (var spouse in spouses)
                {
                    if (!_nodePositions.ContainsKey(spouse.Id)) continue;
                    if (!_personLevels.ContainsKey(person.Id) || !_personLevels.ContainsKey(spouse.Id)) continue;
                    if (_personLevels[person.Id] != _personLevels[spouse.Id]) continue;

                    string key = string.Compare(person.Id, spouse.Id) < 0 
                        ? $"{person.Id}_{spouse.Id}" 
                        : $"{spouse.Id}_{person.Id}";

                    if (drawnMarriages.Contains(key)) continue;
                    drawnMarriages.Add(key);

                    var pos1 = _nodePositions[person.Id];
                    var pos2 = _nodePositions[spouse.Id];

                    float y = pos1.Y + NODE_HEIGHT / 2;
                    float x1 = pos1.X + NODE_WIDTH;
                    float x2 = pos2.X;

                    using (var pen = new Pen(Color.FromArgb(220, 20, 60), 3))
                    {
                        g.DrawLine(pen, x1, y, x2, y);
                    }

                    float heartX = (x1 + x2) / 2;
                    g.FillEllipse(Brushes.Red, heartX - 6, y - 6, 12, 12);
                }
            }

            // 2. Líneas padres-hijos (verticales ordenadas)
            foreach (var person in _familyTree.GetAllMembers())
            {
                if (!_nodePositions.ContainsKey(person.Id)) continue;
                if (!_personLevels.ContainsKey(person.Id)) continue;

                var children = _familyTree.GetChildren(person.Id)
                    .Where(c => _nodePositions.ContainsKey(c.Id) && _personLevels.ContainsKey(c.Id))
                    .ToList();

                if (children.Count == 0) continue;

                string key = person.Id;
                if (drawnParentChild.Contains(key)) continue;
                drawnParentChild.Add(key);

                DrawParentChildLines(g, person, children);
            }
        }

        private void DrawParentChildLines(Graphics g, Person parent, List<Person> children)
        {
            var parentPos = _nodePositions[parent.Id];
            float parentY = parentPos.Y + NODE_HEIGHT;

            // Encontrar punto de inicio (centro de pareja o centro de padre)
            var spouse = _familyTree.GetSpouses(parent.Id)
                .FirstOrDefault(s => _personLevels.ContainsKey(s.Id) && 
                                   _personLevels[s.Id] == _personLevels[parent.Id] &&
                                   _nodePositions.ContainsKey(s.Id));

            float startX;
            if (spouse != null)
            {
                var spousePos = _nodePositions[spouse.Id];
                startX = (parentPos.X + NODE_WIDTH + spousePos.X) / 2;
            }
            else
            {
                startX = parentPos.X + NODE_WIDTH / 2;
            }

            // Posiciones de hijos
            var childCenters = children
                .Select(c => new { 
                    Person = c, 
                    X = _nodePositions[c.Id].X + NODE_WIDTH / 2,
                    Y = _nodePositions[c.Id].Y 
                })
                .OrderBy(c => c.X)
                .ToList();

            float horizontalLineY = parentY + V_SPACING / 2;

            using (var pen = new Pen(Color.FromArgb(70, 130, 180), 3))
            {
                // Línea vertical desde padres hasta línea horizontal
                g.DrawLine(pen, startX, parentY, startX, horizontalLineY);

                if (childCenters.Count > 1)
                {
                    // Línea horizontal conectando todos los hijos
                    float leftX = childCenters.First().X;
                    float rightX = childCenters.Last().X;
                    g.DrawLine(pen, leftX, horizontalLineY, rightX, horizontalLineY);
                }

                // Líneas verticales a cada hijo
                foreach (var child in childCenters)
                {
                    g.DrawLine(pen, child.X, horizontalLineY, child.X, child.Y);
                }
            }
        }

        private void DrawNode(Graphics g, Person person, PointF position)
        {
            var rect = new Rectangle((int)position.X, (int)position.Y, NODE_WIDTH, NODE_HEIGHT);

            // Sombra
            var shadowRect = new Rectangle(rect.X + 3, rect.Y + 3, rect.Width, rect.Height);
            using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
            {
                g.FillRoundedRectangle(shadowBrush, shadowRect, 8);
            }

            // Fondo con gradiente
            var color1 = person.IsAlive ? Color.FromArgb(230, 245, 255) : Color.FromArgb(210, 210, 210);
            var color2 = person.IsAlive ? Color.FromArgb(200, 230, 255) : Color.FromArgb(170, 170, 170);
            
            using (var gradient = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect, color1, color2, System.Drawing.Drawing2D.LinearGradientMode.Vertical))
            {
                g.FillRoundedRectangle(gradient, rect, 8);
            }

            // Borde
            var borderColor = person.IsAlive ? Color.FromArgb(0, 122, 204) : Color.Gray;
            using (var pen = new Pen(borderColor, 2))
            {
                g.DrawRoundedRectangle(pen, rect, 8);
            }

            // Texto
            var fontName = new Font("Segoe UI", 10, FontStyle.Bold);
            var fontSmall = new Font("Segoe UI", 8.5f);
            var fontTiny = new Font("Segoe UI", 7.5f);
            var sf = new StringFormat { Alignment = StringAlignment.Center };

            float centerX = position.X + NODE_WIDTH / 2;
            float textY = position.Y + 10;

            g.DrawString(person.FullName, fontName, Brushes.Black, centerX, textY, sf);
            textY += 22;

            g.DrawString($"🆔 {person.NationalId}", fontSmall, Brushes.DarkSlateGray, centerX, textY, sf);
            textY += 20;

            string ageText = person.IsAlive ? $"🎂 {person.Age} años" : $"✝ {person.Age} años";
            var ageColor = person.IsAlive ? Color.DarkGreen : Color.DarkRed;
            g.DrawString(ageText, fontSmall, new SolidBrush(ageColor), centerX, textY, sf);
            textY += 20;

            g.DrawString($"📍 {person.Address}", fontTiny, Brushes.Navy, centerX, textY, sf);
        }
    }

    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
        {
            using (var path = GetRoundedRectPath(rect, radius))
            {
                g.FillPath(brush, path);
            }
        }

        public static void DrawRoundedRectangle(this Graphics g, Pen pen, Rectangle rect, int radius)
        {
            using (var path = GetRoundedRectPath(rect, radius))
            {
                g.DrawPath(pen, path);
            }
        }

        private static System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}