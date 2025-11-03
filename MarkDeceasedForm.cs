using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WorldMapZoom
{
    public partial class MarkDeceasedForm : Form
    {
        private FamilyTree _familyTree;
        private ComboBox _cmbPerson;
        private DateTimePicker _dtpDeathDate;
        private Button _btnSave;
        private Button _btnCancel;
        private Person _selectedPerson;

        public MarkDeceasedForm(FamilyTree familyTree)
        {
            _familyTree = familyTree;
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = "Marcar Persona como Fallecida";
            Width = 500;
            Height = 300;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            int yPos = 30;

            var lblPerson = new Label
            {
                Text = "Seleccionar persona viva:",
                Location = new Point(30, yPos),
                Width = 200,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            Controls.Add(lblPerson);
            yPos += 30;

            _cmbPerson = new ComboBox
            {
                Location = new Point(30, yPos),
                Width = 420,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            _cmbPerson.SelectedIndexChanged += CmbPerson_SelectedIndexChanged;
            Controls.Add(_cmbPerson);
            yPos += 50;

            var lblDeathDate = new Label
            {
                Text = "Fecha de defunción:",
                Location = new Point(30, yPos),
                Width = 200,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            Controls.Add(lblDeathDate);
            yPos += 30;

            _dtpDeathDate = new DateTimePicker
            {
                Location = new Point(30, yPos),
                Width = 420,
                Format = DateTimePickerFormat.Short,
                MaxDate = DateTime.Today,
                Font = new Font("Segoe UI", 10)
            };
            Controls.Add(_dtpDeathDate);
            yPos += 60;

            _btnSave = new Button
            {
                Text = "Guardar",
                Location = new Point(180, yPos),
                Width = 120,
                Height = 40,
                BackColor = Color.FromArgb(204, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += BtnSave_Click;
            Controls.Add(_btnSave);

            _btnCancel = new Button
            {
                Text = "Cancelar",
                Location = new Point(310, yPos),
                Width = 120,
                Height = 40,
                BackColor = Color.Gray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(_btnCancel);

            LoadLivingPersons();
        }

        private void LoadLivingPersons()
        {
            var livingPersons = _familyTree.GetAllMembers()
                .Where(p => p.IsAlive)
                .OrderBy(p => p.FullName)
                .ToList();

            if (livingPersons.Count == 0)
            {
                MessageBox.Show("No hay personas vivas en el árbol genealógico.", 
                    "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
                return;
            }

            foreach (var person in livingPersons)
            {
                _cmbPerson.Items.Add(new ComboBoxItem
                {
                    Value = person.Id,
                    Display = $"{person.FullName} ({person.NationalId}) - {person.Age} años",
                    Person = person
                });
            }

            if (_cmbPerson.Items.Count > 0)
                _cmbPerson.SelectedIndex = 0;
        }

        private void CmbPerson_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cmbPerson.SelectedItem is ComboBoxItem item)
            {
                _selectedPerson = item.Person;
                _dtpDeathDate.MinDate = _selectedPerson.BirthDate.AddDays(1);
                _dtpDeathDate.Value = DateTime.Today;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_selectedPerson == null)
            {
                MessageBox.Show("Debe seleccionar una persona.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_dtpDeathDate.Value <= _selectedPerson.BirthDate)
            {
                MessageBox.Show("La fecha de defunción debe ser posterior a la fecha de nacimiento.", 
                    "Fecha inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_dtpDeathDate.Value > DateTime.Today)
            {
                MessageBox.Show("La fecha de defunción no puede ser posterior a hoy.", 
                    "Fecha inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"¿Está seguro de marcar a {_selectedPerson.FullName} como fallecido(a)?", 
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _selectedPerson.IsAlive = false;
                _selectedPerson.DeathDate = _dtpDeathDate.Value;
                _selectedPerson.CalculateAge();

                _familyTree.UpdatePerson(_selectedPerson);
                DataLoader.SaveToJson(_familyTree);

                MessageBox.Show("Persona marcada como fallecida y guardada correctamente.", 
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
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