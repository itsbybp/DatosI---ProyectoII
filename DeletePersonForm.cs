using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WorldMapZoom
{
    public partial class DeletePersonForm : Form
    {
        private FamilyTree _familyTree;
        private ComboBox _cmbPerson;
        private Button _btnDelete;
        private Button _btnCancel;
        private Person _selectedPerson;
        private Label _lblWarning;
        private Panel _infoPanel;

        public DeletePersonForm(FamilyTree familyTree)
        {
            _familyTree = familyTree;
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = "Eliminar Persona del Árbol Genealógico";
            Width = 600;
            Height = 500;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            int yPos = 30;

            // Título principal
            var titleLabel = new Label
            {
                Text = "⚠️ ELIMINAR MIEMBRO",
                Location = new Point(30, yPos),
                Width = 540,
                Height = 40,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(192, 57, 43),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(titleLabel);
            yPos += 50;

            var lblPerson = new Label
            {
                Text = "Seleccionar persona a eliminar:",
                Location = new Point(30, yPos),
                Width = 540,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            Controls.Add(lblPerson);
            yPos += 30;

            _cmbPerson = new ComboBox
            {
                Location = new Point(30, yPos),
                Width = 540,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            _cmbPerson.SelectedIndexChanged += CmbPerson_SelectedIndexChanged;
            Controls.Add(_cmbPerson);
            yPos += 50;

            // Panel de información de la persona seleccionada
            _infoPanel = new Panel
            {
                Location = new Point(30, yPos),
                Size = new Size(540, 150),
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            Controls.Add(_infoPanel);
            yPos += 160;

            // Advertencia
            _lblWarning = new Label
            {
                Text = "⚠️ ADVERTENCIA: Esta acción no se puede deshacer.\n\n" +
                       "Al eliminar esta persona también se eliminarán:\n" +
                       "• Todas sus relaciones familiares\n" +
                       "• Referencias en otros miembros del árbol",
                Location = new Point(30, yPos),
                Width = 540,
                Height = 90,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(192, 57, 43),
                BackColor = Color.FromArgb(255, 243, 224),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10),
                Visible = false
            };
            Controls.Add(_lblWarning);
            yPos += 100;

            // Botones
            _btnDelete = new Button
            {
                Text = "🗑️ Eliminar Persona",
                Location = new Point(180, yPos),
                Width = 180,
                Height = 45,
                BackColor = Color.FromArgb(192, 57, 43),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _btnDelete.FlatAppearance.BorderSize = 0;
            _btnDelete.Click += BtnDelete_Click;
            Controls.Add(_btnDelete);

            _btnCancel = new Button
            {
                Text = "Cancelar",
                Location = new Point(370, yPos),
                Width = 180,
                Height = 45,
                BackColor = Color.Gray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(_btnCancel);

            LoadPersons();
        }

        private void LoadPersons()
        {
            var allPersons = _familyTree.GetAllMembers()
                .OrderBy(p => p.FullName)
                .ToList();

            if (allPersons.Count == 0)
            {
                MessageBox.Show("No hay personas en el árbol genealógico.", 
                    "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
                return;
            }

            _cmbPerson.Items.Clear();
            _cmbPerson.Items.Add("-- Seleccione una persona --");

            foreach (var person in allPersons)
            {
                string status = person.IsAlive ? "✓" : "✝";
                _cmbPerson.Items.Add(new ComboBoxItem
                {
                    Value = person.Id,
                    Display = $"{status} {person.FullName} ({person.NationalId}) - {person.Age} años",
                    Person = person
                });
            }

            _cmbPerson.SelectedIndex = 0;
        }

        private void CmbPerson_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cmbPerson.SelectedIndex <= 0 || _cmbPerson.SelectedItem == null)
            {
                _selectedPerson = null;
                _infoPanel.Visible = false;
                _lblWarning.Visible = false;
                _btnDelete.Enabled = false;
                return;
            }

            if (_cmbPerson.SelectedItem is ComboBoxItem item)
            {
                _selectedPerson = item.Person;
                UpdateInfoPanel(_selectedPerson);
                _infoPanel.Visible = true;
                _lblWarning.Visible = true;
                _btnDelete.Enabled = true;
            }
        }

        private void UpdateInfoPanel(Person person)
        {
            _infoPanel.Controls.Clear();

            int yPos = 10;

            var lblName = new Label
            {
                Text = $"📋 {person.FullName}",
                Location = new Point(10, yPos),
                Width = 520,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            _infoPanel.Controls.Add(lblName);
            yPos += 30;

            var info = new[]
            {
                $"🆔 Cédula: {person.NationalId}",
                $"📅 Edad: {person.Age} años",
                $"📍 Dirección: {person.Address}"
            };

            foreach (var line in info)
            {
                var lbl = new Label
                {
                    Text = line,
                    Location = new Point(10, yPos),
                    Width = 520,
                    Font = new Font("Segoe UI", 9)
                };
                _infoPanel.Controls.Add(lbl);
                yPos += 22;
            }

            // Mostrar relaciones
            var parents = _familyTree.GetParents(person.Id);
            var spouses = _familyTree.GetSpouses(person.Id);
            var children = _familyTree.GetChildren(person.Id);

            int totalRelations = parents.Count + spouses.Count + children.Count;

            var lblRelations = new Label
            {
                Text = $"👥 Relaciones: {parents.Count} padre(s), {spouses.Count} cónyuge(s), {children.Count} hijo(s)",
                Location = new Point(10, yPos),
                Width = 520,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(231, 76, 60)
            };
            _infoPanel.Controls.Add(lblRelations);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedPerson == null)
            {
                MessageBox.Show("Debe seleccionar una persona.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Mensaje de confirmación
            var result = MessageBox.Show(
                $"¿Está completamente seguro de que desea eliminar a:\n\n" +
                $"👤 {_selectedPerson.FullName}\n" +
                $"🆔 {_selectedPerson.NationalId}\n\n" +
                $"Esta acción eliminará permanentemente a esta persona y todas sus relaciones.\n\n" +
                $"⚠️ ESTA ACCIÓN NO SE PUEDE DESHACER ⚠️",
                "⚠️ CONFIRMACIÓN DE ELIMINACIÓN", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes)
                return;

            // Segunda confirmación
            var secondConfirm = MessageBox.Show(
                $"¿Realmente desea continuar con la eliminación de {_selectedPerson.FullName}?",
                "Última Confirmación", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (secondConfirm != DialogResult.Yes)
                return;

            try
            {
                // Eliminar la foto de la persona antes de eliminarla del árbol
                DeletePersonPhoto(_selectedPerson);

                // Eliminar la persona usando el método de FamilyTree
                _familyTree.RemovePerson(_selectedPerson.Id);

                // Guardar cambios
                DataLoader.SaveToJson(_familyTree);

                MessageBox.Show(
                    $"{_selectedPerson.FullName} ha sido eliminado(a) correctamente del árbol genealógico.", 
                    "✓ Eliminación Exitosa", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al eliminar la persona: {ex.Message}", 
                    "Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }

        private void DeletePersonPhoto(Person person)
        {
            try
            {
                // Solo eliminar si la foto es local (en carpeta Images)
                if (!string.IsNullOrEmpty(person.PhotoUrl) && person.PhotoUrl.StartsWith("Images/"))
                {
                    string photoPath = Path.Combine(Environment.CurrentDirectory, person.PhotoUrl);
                    if (File.Exists(photoPath))
                    {
                        File.Delete(photoPath);
                        System.Diagnostics.Debug.WriteLine($"Foto eliminada: {photoPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                // No mostrar error al usuario, solo registrar en debug
                System.Diagnostics.Debug.WriteLine($"Error al eliminar foto: {ex.Message}");
                // La eliminación de la persona continúa aunque falle la eliminación de la foto
            }
        }

        private class ComboBoxItem
        {
            public string Value { get; set; }
            public string Display { get; set; }
            public Person Person { get; set; }

            public override string ToString()
            {
                return Display;
            }
        }
    }
}
