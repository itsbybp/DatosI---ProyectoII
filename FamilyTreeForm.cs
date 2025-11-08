using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WorldMapZoom
{
    public partial class FamilyTreeForm : Form
    {
        private FamilyTree _familyTree;
        private Panel _drawBox, _sidePanel;
        private Dictionary<string, PointF> _nodePositions = new Dictionary<string, PointF>();
        private Dictionary<string, int> _generations = new Dictionary<string, int>();
        private Dictionary<string, Image> _photoCache = new Dictionary<string, Image>();
        private Person _selectedPerson;

        // Zoom y Pan
        private float _zoomLevel = 1.0f;
        private const float MIN_ZOOM = 0.1f, MAX_ZOOM = 3.0f, ZOOM_STEP = 0.1f;
        private PointF _panOffset = new PointF(0, 0);
        private Point _lastMousePos;
        private bool _isPanning = false;

        // Constantes de diseño
        private const float BUBBLE_DIAMETER = 100f, HORIZONTAL_SPACING = 150f, VERTICAL_SPACING = 280f;
        private const float COUPLE_SPACING = 50f, FAMILY_GROUP_SPACING = 250f, MARGIN = 150f;

        public FamilyTreeForm(FamilyTree familyTree)
        {
            _familyTree = familyTree;
            InitializeComponent();
            InitializeCustomComponents();
            CalculateGenerationsFixed();
            CalculatePositionsImproved();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Árbol Genealógico - Usa rueda del mouse para zoom";
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(245, 245, 250);

            _drawBox = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _drawBox.GetType().InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, _drawBox, new object[] { true });

            _drawBox.Paint += DrawBox_Paint;
            _drawBox.MouseClick += DrawBox_MouseClick;
            _drawBox.MouseWheel += DrawBox_MouseWheel;
            _drawBox.MouseDown += DrawBox_MouseDown;
            _drawBox.MouseMove += DrawBox_MouseMove;
            _drawBox.MouseUp += DrawBox_MouseUp;

            _sidePanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 350,
                BackColor = Color.FromArgb(248, 249, 250),
                Visible = false,
                Padding = new Padding(10),
                AutoScroll = true
            };

            this.Controls.AddRange(new Control[] { _drawBox, _sidePanel });
        }

        private void DrawBox_MouseWheel(object sender, MouseEventArgs e)
        {
            float oldZoom = _zoomLevel;
            _zoomLevel = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, _zoomLevel + (e.Delta > 0 ? ZOOM_STEP : -ZOOM_STEP)));

            if (_zoomLevel != oldZoom)
            {
                float zoomFactor = _zoomLevel / oldZoom;
                _panOffset.X = e.X - (e.X - _panOffset.X) * zoomFactor;
                _panOffset.Y = e.Y - (e.Y - _panOffset.Y) * zoomFactor;
            }
            _drawBox.Invalidate();
        }

        private void DrawBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Right)
            {
                _isPanning = true;
                _lastMousePos = e.Location;
                _drawBox.Cursor = Cursors.SizeAll;
            }
        }

        private void DrawBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                _panOffset.X += e.X - _lastMousePos.X;
                _panOffset.Y += e.Y - _lastMousePos.Y;
                _lastMousePos = e.Location;
                _drawBox.Invalidate();
            }
        }

        private void DrawBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Right)
            {
                _isPanning = false;
                _drawBox.Cursor = Cursors.Default;
            }
        }

        private void CalculateGenerationsFixed()
        {
            _generations.Clear();
            var allMembers = _familyTree.GetAllMembers();
            if (allMembers.Count == 0) return;

            Console.WriteLine($"=== CALCULANDO GENERACIONES ===\nTotal personas: {allMembers.Count}");

            var potentialFounders = allMembers.Where(p => p.ParentIds == null || p.ParentIds.Count == 0).ToList();
            var explicitFounders = potentialFounders.Where(p => p.Id.Contains("gen0")).ToList();
            var trueFounders = explicitFounders.Count > 0 ? explicitFounders : potentialFounders.OrderBy(p => p.BirthDate).Take(2).ToList();

            foreach (var founder in trueFounders)
            {
                _generations[founder.Id] = 0;
                Console.WriteLine($"Gen 0: {founder.FullName}");

                if (founder.SpouseIds != null)
                {
                    foreach (var spouseId in founder.SpouseIds.Where(sid => !_generations.ContainsKey(sid)))
                    {
                        _generations[spouseId] = 0;
                        Console.WriteLine($"Gen 0: {_familyTree.GetPerson(spouseId)?.FullName} (cónyuge)");
                    }
                }
            }

            bool changed = true;
            for (int iter = 0; iter < 20 && changed; iter++)
            {
                changed = false;
                foreach (var person in allMembers.Where(p => !_generations.ContainsKey(p.Id) && p.ParentIds?.Count > 0))
                {
                    var parentGens = person.ParentIds.Where(pid => _generations.ContainsKey(pid)).Select(pid => _generations[pid]).ToList();
                    if (parentGens.Count > 0)
                    {
                        _generations[person.Id] = parentGens.Max() + 1;
                        Console.WriteLine($"Gen {_generations[person.Id]}: {person.FullName}");
                        changed = true;

                        if (person.SpouseIds != null)
                        {
                            foreach (var spouseId in person.SpouseIds.Where(sid => !_generations.ContainsKey(sid)))
                            {
                                _generations[spouseId] = _generations[person.Id];
                                Console.WriteLine($"Gen {_generations[spouseId]}: {_familyTree.GetPerson(spouseId)?.FullName} (cónyuge)");
                            }
                        }
                    }
                }
            }

            foreach (var person in allMembers.Where(p => !_generations.ContainsKey(p.Id)))
            {
                _generations[person.Id] = -1;
                Console.WriteLine($"Gen -1: {person.FullName} (sin conexión)");
            }

            Console.WriteLine($"=== FIN CÁLCULO GENERACIONES ===");
        }

        private void CalculatePositionsImproved()
        {
            _nodePositions.Clear();
            var allMembers = _familyTree.GetAllMembers();
            if (allMembers.Count == 0) return;

            var generations = _generations.GroupBy(kvp => kvp.Value).OrderBy(g => g.Key).ToList();
            float currentY = MARGIN;

            foreach (var gen in generations)
            {
                var genPeople = gen.Select(kvp => _familyTree.GetPerson(kvp.Key)).Where(p => p != null).ToList();
                var familyGroups = new List<List<Person>>();
                var processed = new HashSet<string>();

                foreach (var person in genPeople.Where(p => !processed.Contains(p.Id)))
                {
                    var group = BuildFamilyGroupIterative(person, processed);
                    if (group.Count > 0) familyGroups.Add(group);
                }

                float totalWidth = familyGroups.Sum(fg => CalculateGroupWidth(fg)) + (familyGroups.Count - 1) * FAMILY_GROUP_SPACING;
                float currentX = (Screen.PrimaryScreen.WorkingArea.Width - totalWidth) / 2;

                foreach (var group in familyGroups)
                {
                    PlaceFamilyGroup(group, ref currentX, currentY);
                    currentX += FAMILY_GROUP_SPACING;
                }

                currentY += VERTICAL_SPACING;
            }

            Console.WriteLine($"Posiciones calculadas: {_nodePositions.Count}");
        }

        private List<Person> BuildFamilyGroupIterative(Person root, HashSet<string> processed)
        {
            var group = new List<Person>();
            var queue = new Queue<Person>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (processed.Contains(current.Id)) continue;

                processed.Add(current.Id);
                group.Add(current);

                var spouses = _familyTree.GetSpouses(current.Id).Where(s => !processed.Contains(s.Id) &&
                    _generations.ContainsKey(s.Id) && _generations[s.Id] == _generations[current.Id]);
                foreach (var spouse in spouses) queue.Enqueue(spouse);
            }
            return group;
        }

        private float CalculateGroupWidth(List<Person> group) =>
            group.Count == 1 ? BUBBLE_DIAMETER : (group.Count - 1) * COUPLE_SPACING + group.Count * BUBBLE_DIAMETER;

        private void PlaceFamilyGroup(List<Person> group, ref float x, float y)
        {
            foreach (var person in group)
            {
                _nodePositions[person.Id] = new PointF(x, y);
                Console.WriteLine($"Posición: {person.FullName} en ({x}, {y})");
                x += BUBBLE_DIAMETER + COUPLE_SPACING;
            }
            x -= COUPLE_SPACING;
        }

        private void DrawBox_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.Clear(Color.White);
            e.Graphics.ScaleTransform(_zoomLevel, _zoomLevel);
            e.Graphics.TranslateTransform(_panOffset.X / _zoomLevel, _panOffset.Y / _zoomLevel);

            DrawRelationships(e.Graphics);
            DrawNodes(e.Graphics);
        }

        private void DrawRelationships(Graphics g)
        {
            var drawnSpouseLinks = new HashSet<string>();
            using (var pen = new Pen(Color.FromArgb(200, 200, 200), 2))
            using (var redPen = new Pen(Color.FromArgb(220, 53, 69), 3))
            {
                foreach (var kvp in _nodePositions)
                {
                    var person = _familyTree.GetPerson(kvp.Key);
                    if (person == null) continue;

                    var pos = kvp.Value;
                    var spouses = _familyTree.GetSpouses(person.Id);

                    foreach (var spouse in spouses.Where(s => _nodePositions.ContainsKey(s.Id)))
                    {
                        string key = string.Compare(person.Id, spouse.Id) < 0 ? $"{person.Id}-{spouse.Id}" : $"{spouse.Id}-{person.Id}";
                        if (drawnSpouseLinks.Contains(key)) continue;

                        var spousePos = _nodePositions[spouse.Id];
                        // Línea roja entre cónyuges
                        g.DrawLine(redPen, pos.X + BUBBLE_DIAMETER / 2, pos.Y + BUBBLE_DIAMETER / 2,
                            spousePos.X + BUBBLE_DIAMETER / 2, spousePos.Y + BUBBLE_DIAMETER / 2);
                        drawnSpouseLinks.Add(key);
                    }

                    var children = _familyTree.GetChildren(person.Id);
                    if (children.Count == 0) continue;

                    var childrenInTree = children.Where(c => _nodePositions.ContainsKey(c.Id)).ToList();
                    if (childrenInTree.Count == 0) continue;

                    float baseX = spouses.Count > 0 && _nodePositions.ContainsKey(spouses[0].Id)
                        ? (pos.X + _nodePositions[spouses[0].Id].X) / 2 + BUBBLE_DIAMETER / 2
                        : pos.X + BUBBLE_DIAMETER / 2;
                    float baseY = pos.Y + BUBBLE_DIAMETER;

                    // Calcular el punto medio vertical hacia los hijos
                    float minChildY = childrenInTree.Min(c => _nodePositions[c.Id].Y);
                    float midY = baseY + (minChildY - baseY) / 2;

                    // Línea vertical gris desde la pareja hasta el punto medio
                    g.DrawLine(pen, baseX, baseY, baseX, midY);

                    // Dibujar líneas diagonales desde el punto medio hacia cada hijo
                    foreach (var child in childrenInTree)
                    {
                        var childPos = _nodePositions[child.Id];
                        float childX = childPos.X + BUBBLE_DIAMETER / 2;
                        float childY = childPos.Y;
                        g.DrawLine(pen, baseX, midY, childX, childY);
                    }
                }
            }
        }

        private void DrawNodes(Graphics g)
        {
            foreach (var kvp in _nodePositions)
            {
                var person = _familyTree.GetPerson(kvp.Key);
                if (person == null) continue;

                var pos = kvp.Value;
                bool isSelected = _selectedPerson?.Id == person.Id;

                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(pos.X, pos.Y, BUBBLE_DIAMETER, BUBBLE_DIAMETER);
                    using (var pathBrush = new PathGradientBrush(path))
                    {
                        pathBrush.CenterColor = Color.FromArgb(173, 216, 230);
                        pathBrush.SurroundColors = new[] { Color.FromArgb(135, 206, 250) };
                        g.FillPath(pathBrush, path);
                    }

                    using (var borderPen = new Pen(isSelected ? Color.FromArgb(255, 193, 7) : (person.IsAlive ? Color.FromArgb(40, 167, 69) : Color.FromArgb(220, 53, 69)), isSelected ? 4 : 2))
                    {
                        g.DrawPath(borderPen, path);
                    }
                }

                Image photo = LoadPhoto(person);
                if (photo != null)
                {
                    using (var path = new GraphicsPath())
                    {
                        path.AddEllipse(pos.X + 10, pos.Y + 10, BUBBLE_DIAMETER - 20, BUBBLE_DIAMETER - 20);
                        var clip = g.Clip;
                        g.SetClip(path);
                        g.DrawImage(photo, pos.X + 10, pos.Y + 10, BUBBLE_DIAMETER - 20, BUBBLE_DIAMETER - 20);
                        g.Clip = clip;
                    }
                }
                else
                {
                    // Solo mostrar iniciales si no hay foto
                    string initials = $"{person.FirstName[0]}{person.FirstLastName[0]}";
                    using (var font = new Font("Arial", 18, FontStyle.Bold))
                    using (var brush = new SolidBrush(Color.White))
                    using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        g.DrawString(initials, font, brush, new RectangleF(pos.X, pos.Y, BUBBLE_DIAMETER, BUBBLE_DIAMETER), format);
                    }
                }

                string displayName = person.FirstName.Length > 12 ? person.FirstName.Substring(0, 12) + "..." : person.FirstName;
                using (var font = new Font("Segoe UI", 9, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.FromArgb(33, 37, 41)))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(displayName, font, brush, new RectangleF(pos.X - 25, pos.Y + BUBBLE_DIAMETER + 5, BUBBLE_DIAMETER + 50, 20), format);
                    g.DrawString($"{person.Age} años", font, brush, new RectangleF(pos.X - 25, pos.Y + BUBBLE_DIAMETER + 22, BUBBLE_DIAMETER + 50, 20), format);
                }
            }
        }

        private Image LoadPhoto(Person person)
        {
            if (string.IsNullOrWhiteSpace(person.PhotoUrl)) return null;
            if (_photoCache.ContainsKey(person.Id)) return _photoCache[person.Id];

            try
            {
                using (var client = new HttpClient())
                {
                    var data = client.GetByteArrayAsync(person.PhotoUrl).Result;
                    using (var ms = new System.IO.MemoryStream(data))
                    {
                        _photoCache[person.Id] = Image.FromStream(ms);
                        return _photoCache[person.Id];
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private void DrawBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            float worldX = (e.X - _panOffset.X) / _zoomLevel;
            float worldY = (e.Y - _panOffset.Y) / _zoomLevel;

            foreach (var kvp in _nodePositions)
            {
                var pos = kvp.Value;
                float dx = worldX - (pos.X + BUBBLE_DIAMETER / 2);
                float dy = worldY - (pos.Y + BUBBLE_DIAMETER / 2);

                if (Math.Sqrt(dx * dx + dy * dy) <= BUBBLE_DIAMETER / 2)
                {
                    ShowPersonDetails(kvp.Key);
                    return;
                }
            }
        }

        private void ShowPersonDetails(string personId)
        {
            var person = _familyTree.GetPerson(personId);
            if (person == null) return;

            _selectedPerson = person;
            _drawBox.Invalidate();
            _sidePanel.Controls.Clear();
            _sidePanel.Visible = true;

            int yPos = 10;

            var photoPanel = new Panel { Location = new Point(105, yPos), Size = new Size(140, 140), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            Image photo = LoadPhoto(person);
            if (photo != null)
            {
                var pb = new PictureBox { Dock = DockStyle.Fill, Image = photo, SizeMode = PictureBoxSizeMode.Zoom };
                photoPanel.Controls.Add(pb);
            }
            _sidePanel.Controls.Add(photoPanel);
            yPos += 155;

            var lblName = new Label
            {
                Text = person.FullName,
                Location = new Point(20, yPos),
                Size = new Size(310, 40),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            _sidePanel.Controls.Add(lblName);
            yPos += 50;

            AddSectionHeader("📋 INFORMACIÓN BÁSICA", ref yPos);
            AddInfoField("Edad:", $"{person.Age} años", ref yPos);
            AddInfoField("Fecha Nac.:", person.BirthDate.ToString("dd/MM/yyyy"), ref yPos);
            AddInfoField("Estado:", person.IsAlive ? "Vivo" : "Fallecido", ref yPos);

            if (!person.IsAlive && person.DeathDate.HasValue)
                AddInfoField("Defunción:", person.DeathDate.Value.ToString("dd/MM/yyyy"), ref yPos);

            AddInfoField("Cédula:", person.NationalId ?? "No registrada", ref yPos);
            AddInfoField("Generación:", _generations.ContainsKey(person.Id) ? _generations[person.Id].ToString() : "N/A", ref yPos);
            yPos += 20;

            AddRelationSection("👨‍👩 PADRES", _familyTree.GetParents(person.Id), ref yPos);
            AddRelationSection("💑 CÓNYUGE(S)", _familyTree.GetSpouses(person.Id), ref yPos);
            AddRelationSection("👶 HIJOS", _familyTree.GetChildren(person.Id).OrderBy(c => c.BirthDate).ToList(), ref yPos);

            var btnClose = new Button
            {
                Text = "✕ Cerrar",
                Location = new Point(20, yPos + 10),
                Size = new Size(310, 40),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => _sidePanel.Visible = false;
            _sidePanel.Controls.Add(btnClose);

            _sidePanel.AutoScrollMinSize = new Size(0, yPos + 70);
        }

        private void AddRelationSection(string title, List<Person> people, ref int yPos)
        {
            AddSectionHeader(title, ref yPos);
            if (people.Count > 0)
                foreach (var p in people) AddPersonCard(p, ref yPos);
            else
                AddEmptyMessage("Sin " + (title.Contains("PADRES") ? "padres" : title.Contains("CÓNYUGE") ? "cónyuge" : "hijos") + " registrados", ref yPos);
            yPos += 20;
        }

        private void AddSectionHeader(string title, ref int yPos)
        {
            _sidePanel.Controls.Add(new Label
            {
                Text = title,
                Location = new Point(20, yPos),
                Size = new Size(310, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 58, 64),
                BackColor = Color.Transparent
            });
            yPos += 30;
            _sidePanel.Controls.Add(new Panel { Location = new Point(20, yPos), Size = new Size(310, 2), BackColor = Color.FromArgb(222, 226, 230) });
            yPos += 15;
        }

        private void AddInfoField(string label, string value, ref int yPos)
        {
            var lblLabel = new Label
            {
                Text = label,
                Location = new Point(20, yPos),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(108, 117, 125),
                BackColor = Color.Transparent
            };
            var lblValue = new Label
            {
                Text = value,
                Location = new Point(130, yPos),
                MaximumSize = new Size(190, 0),
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(33, 37, 41),
                BackColor = Color.Transparent
            };
            _sidePanel.Controls.AddRange(new Control[] { lblLabel, lblValue });
            yPos += Math.Max(lblLabel.Height, lblValue.Height) + 10;
        }

        private void AddPersonCard(Person person, ref int yPos)
        {
            var card = new Panel
            {
                Location = new Point(20, yPos),
                Size = new Size(310, 60),
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };

            bool isAlive = person.IsAlive;
            var lblStatus = new Label
            {
                Text = isAlive ? "✓" : "✝",
                Location = new Point(10, 15),
                Size = new Size(20, 20),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = isAlive ? Color.FromArgb(40, 167, 69) : Color.FromArgb(220, 53, 69),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            var lblName = new Label
            {
                Text = person.FullName,
                Location = new Point(40, 8),
                MaximumSize = new Size(260, 0),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 37, 41),
                BackColor = Color.Transparent
            };
            var lblAge = new Label
            {
                Text = $"{person.Age} años",
                Location = new Point(40, lblName.Height + 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(108, 117, 125),
                BackColor = Color.Transparent
            };

            card.Controls.AddRange(new Control[] { lblStatus, lblName, lblAge });
            int cardHeight = Math.Max(60, lblName.Height + lblAge.Height + 20);
            card.Height = cardHeight;

            card.Click += (s, e) => ShowPersonDetails(person.Id);
            foreach (Control ctrl in card.Controls) ctrl.Click += (s, e) => ShowPersonDetails(person.Id);

            _sidePanel.Controls.Add(card);
            yPos += cardHeight + 10;
        }

        private void AddEmptyMessage(string message, ref int yPos)
        {
            _sidePanel.Controls.Add(new Label
            {
                Text = message,
                Location = new Point(20, yPos),
                Size = new Size(310, 30),
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(173, 181, 189),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });
            yPos += 40;
        }
    }
}