using System;
using System.Drawing;
using System.Windows.Forms;

namespace WorldMapZoom
{
    public partial class StatisticsForm : Form
    {
        private FamilyTree _familyTree;
        private Panel _mainPanel;

        public StatisticsForm(FamilyTree familyTree)
        {
            _familyTree = familyTree;
            InitializeComponent();
            InitializeUI();
            CalculateStatistics();
        }

        private void InitializeUI()
        {
            Text = "Estadísticas del Árbol Genealógico";
            Width = 700;
            Height = 550;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.White;

            _mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };
            Controls.Add(_mainPanel);

            var titleLabel = new Label
            {
                Text = "ESTADÍSTICAS DE DISTANCIAS",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 122, 204),
                AutoSize = false,
                Size = new Size(660, 50),
                Location = new Point(20, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _mainPanel.Controls.Add(titleLabel);
        }

        private void CalculateStatistics()
        {
            try
            {
                var graph = _familyTree.GetGraph();
                
                if (_familyTree.GetAllMembers().Count < 2)
                {
                    AddStatisticLabel("⚠ Se necesitan al menos 2 personas para calcular estadísticas", 90, Color.DarkOrange);
                    return;
                }

                int yPos = 90;

                // Par más lejano
                var (farthest1, farthest2, maxDist) = graph.GetFarthestPair();
                yPos = AddStatisticSection(
                    "👥 FAMILIARES MÁS DISTANTES",
                    farthest1 != null && farthest2 != null
                        ? $"{farthest1.FullName}\n({farthest1.Address})\n\n↕\n\n{farthest2.FullName}\n({farthest2.Address})\n\n📏 Distancia: {maxDist:F2} km"
                        : "No hay suficientes datos",
                    yPos,
                    Color.DarkRed
                );

                yPos += 20;

                // Par más cercano
                var (closest1, closest2, minDist) = graph.GetClosestPair();
                yPos = AddStatisticSection(
                    "👥 FAMILIARES MÁS CERCANOS",
                    closest1 != null && closest2 != null
                        ? $"{closest1.FullName}\n({closest1.Address})\n\n↕\n\n{closest2.FullName}\n({closest2.Address})\n\n📏 Distancia: {minDist:F2} km"
                        : "No hay suficientes datos",
                    yPos,
                    Color.DarkGreen
                );

                yPos += 20;

                // Distancia promedio
                double avgDist = graph.GetAverageDistance();
                yPos = AddStatisticSection(
                    "📊 DISTANCIA PROMEDIO",
                    $"{avgDist:F1} km",
                    yPos,
                    Color.DarkBlue,
                    60  // Altura reducida para panel de una línea
                );

                // Agregar un control invisible para forzar espacio al final
                var spacer = new Label
                {
                    Location = new Point(0, yPos),
                    Size = new Size(1, 60),
                    BackColor = Color.Transparent
                };
                _mainPanel.Controls.Add(spacer);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al calcular estadísticas: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int AddStatisticSection(string title, string content, int yPos, Color titleColor, int? customHeight = null)
        {
            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = titleColor,
                Location = new Point(30, yPos),
                AutoSize = true
            };
            _mainPanel.Controls.Add(lblTitle);
            yPos += 35;

            // Calcular altura necesaria según el contenido o usar altura personalizada
            int lineCount = content.Split('\n').Length;
            int panelHeight = customHeight ?? Math.Max(140, lineCount * 24 + 30);

            var panel = new Panel
            {
                Location = new Point(40, yPos),
                Size = new Size(600, panelHeight),
                BackColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.FixedSingle
            };
            _mainPanel.Controls.Add(panel);

            var lblContent = new Label
            {
                Text = content,
                Font = new Font("Segoe UI", 10),
                Location = new Point(10, 10),
                AutoSize = false,
                Size = new Size(580, panelHeight - 20),
                TextAlign = ContentAlignment.TopCenter
            };
            panel.Controls.Add(lblContent);

            return yPos + panelHeight + 10;
        }

        private void AddStatisticLabel(string text, int yPos, Color color)
        {
            var label = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 12),
                ForeColor = color,
                Location = new Point(50, yPos),
                AutoSize = false,
                Size = new Size(600, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _mainPanel.Controls.Add(label);
        }
    }
}