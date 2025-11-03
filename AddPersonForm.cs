using System;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using System.Linq;

namespace WorldMapZoom
{
    public partial class AddPersonForm : Form
    {
        private FamilyTree _familyTree;
        private TextBox _txtFirstName;
        private TextBox _txtSecondName;
        private TextBox _txtFirstLastName;
        private TextBox _txtSecondLastName;
        private TextBox _txtNationalId;
        private DateTimePicker _dtpBirthDate;
        private CheckBox _chkDeceased;
        private DateTimePicker _dtpDeathDate;
        private Label _lblDeathDate;
        private TextBox _txtPhotoUrl;
        private TextBox _txtLatitude;
        private TextBox _txtLongitude;
        private TextBox _txtAddress;
        private ComboBox _cmbParent1;
        private ComboBox _cmbParent2;
        private ComboBox _cmbSpouse;
        private Button _btnSave;
        private Button _btnCancel;

        public AddPersonForm(FamilyTree familyTree)
        {
            _familyTree = familyTree;
            InitializeComponent();
            InitializeUI();
            LoadComboBoxes();
        }

        private void InitializeUI()
        {
            Text = "Agregar Persona";
            Width = 550;
            Height = 750;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            int yPos = 20;
            int labelWidth = 140;
            int textBoxX = labelWidth + 30;
            int textBoxWidth = 320;

            AddLabel("Primer Nombre *:", 20, yPos, labelWidth);
            _txtFirstName = AddTextBox(textBoxX, yPos, textBoxWidth);
            yPos += 40;

            AddLabel("Segundo Nombre:", 20, yPos, labelWidth);
            _txtSecondName = AddTextBox(textBoxX, yPos, textBoxWidth);
            yPos += 40;

            AddLabel("Primer Apellido *:", 20, yPos, labelWidth);
            _txtFirstLastName = AddTextBox(textBoxX, yPos, textBoxWidth);
            yPos += 40;

            AddLabel("Segundo Apellido *:", 20, yPos, labelWidth);
            _txtSecondLastName = AddTextBox(textBoxX, yPos, textBoxWidth);
            yPos += 40;

            AddLabel("Cédula *:", 20, yPos, labelWidth);
            _txtNationalId = AddTextBox(textBoxX, yPos, textBoxWidth);
            yPos += 40;

            AddLabel("Fecha Nacimiento *:", 20, yPos, labelWidth);
            _dtpBirthDate = new DateTimePicker
            {
                Location = new Point(textBoxX, yPos),
                Width = textBoxWidth,
                Format = DateTimePickerFormat.Short,
                MaxDate = DateTime.Today
            };
            Controls.Add(_dtpBirthDate);
            yPos += 40;

            _chkDeceased = new CheckBox
            {
                Text = "La persona ha fallecido",
                Location = new Point(textBoxX, yPos),
                Width = textBoxWidth,
                Checked = false
            };
            _chkDeceased.CheckedChanged += ChkDeceased_CheckedChanged;
            Controls.Add(_chkDeceased);
            yPos += 35;

            _lblDeathDate = AddLabel("Fecha Defunción *:", 20, yPos, labelWidth);
            _lblDeathDate.Visible = false;
            _dtpDeathDate = new DateTimePicker
            {
                Location = new Point(textBoxX, yPos),
                Width = textBoxWidth,
                Format = DateTimePickerFormat.Short,
                MaxDate = DateTime.Today,
                Visible = false
            };
            Controls.Add(_dtpDeathDate);
            yPos += 40;

            AddLabel("URL Foto *:", 20, yPos, labelWidth);
            _txtPhotoUrl = AddTextBox(textBoxX, yPos, textBoxWidth);
            _txtPhotoUrl.Text = "https://randomuser.me/api/portraits/men/1.jpg";
            yPos += 40;

            AddLabel("Latitud *:", 20, yPos, labelWidth);
            _txtLatitude = AddTextBox(textBoxX, yPos, textBoxWidth);
            _txtLatitude.Text = "9,935";
            yPos += 40;

            AddLabel("Longitud *:", 20, yPos, labelWidth);
            _txtLongitude = AddTextBox(textBoxX, yPos, textBoxWidth);
            _txtLongitude.Text = "-84,091";
            yPos += 40;

            AddLabel("Dirección *:", 20, yPos, labelWidth);
            _txtAddress = AddTextBox(textBoxX, yPos, textBoxWidth);
            yPos += 40;

            AddLabel("Padre/Madre 1 *:", 20, yPos, labelWidth);
            _cmbParent1 = new ComboBox
            {
                Location = new Point(textBoxX, yPos),
                Width = textBoxWidth,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Controls.Add(_cmbParent1);
            yPos += 40;

            AddLabel("Padre/Madre 2 *:", 20, yPos, labelWidth);
            _cmbParent2 = new ComboBox
            {
                Location = new Point(textBoxX, yPos),
                Width = textBoxWidth,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Controls.Add(_cmbParent2);
            yPos += 40;

            AddLabel("Cónyuge:", 20, yPos, labelWidth);
            _cmbSpouse = new ComboBox
            {
                Location = new Point(textBoxX, yPos),
                Width = textBoxWidth,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Controls.Add(_cmbSpouse);
            yPos += 50;

            _btnSave = new Button
            {
                Text = "Guardar",
                Location = new Point(220, yPos),
                Width = 120,
                Height = 40,
                BackColor = Color.FromArgb(0, 122, 204),
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
                Location = new Point(350, yPos),
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
        }

        private void ChkDeceased_CheckedChanged(object sender, EventArgs e)
        {
            _lblDeathDate.Visible = _chkDeceased.Checked;
            _dtpDeathDate.Visible = _chkDeceased.Checked;
            
            if (_chkDeceased.Checked)
            {
                _dtpDeathDate.MinDate = _dtpBirthDate.Value.AddDays(1);
                _dtpDeathDate.MaxDate = DateTime.Today;
                _dtpDeathDate.Value = DateTime.Today;
            }
        }

        private Label AddLabel(string text, int x, int y, int width)
        {
            var label = new Label
            {
                Text = text,
                Location = new Point(x, y + 3),
                Width = width,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9)
            };
            Controls.Add(label);
            return label;
        }

        private TextBox AddTextBox(int x, int y, int width)
        {
            var textBox = new TextBox
            {
                Location = new Point(x, y),
                Width = width,
                Font = new Font("Segoe UI", 9)
            };
            Controls.Add(textBox);
            return textBox;
        }

        private void LoadComboBoxes()
        {
            _cmbParent1.Items.Add("(Ninguno)");
            _cmbParent2.Items.Add("(Ninguno)");
            _cmbSpouse.Items.Add("(Ninguno)");

            _cmbParent1.SelectedIndex = 0;
            _cmbParent2.SelectedIndex = 0;
            _cmbSpouse.SelectedIndex = 0;

            foreach (var person in _familyTree.GetAllMembers())
            {
                string displayName = $"{person.FullName} ({person.NationalId})";
                _cmbParent1.Items.Add(new ComboBoxItem(person.Id, displayName));
                _cmbParent2.Items.Add(new ComboBoxItem(person.Id, displayName));
                _cmbSpouse.Items.Add(new ComboBoxItem(person.Id, displayName));
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Validar primer nombre
            if (string.IsNullOrWhiteSpace(_txtFirstName.Text))
            {
                MessageBox.Show("El primer nombre es obligatorio.", "Campo requerido", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtFirstName.Focus();
                return;
            }

            if (!EsNombreValido(_txtFirstName.Text))
            {
                MessageBox.Show("El primer nombre solo debe contener letras.", "Nombre inválido", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtFirstName.Focus();
                return;
            }

            // Validar primer apellido
            if (string.IsNullOrWhiteSpace(_txtFirstLastName.Text))
            {
                MessageBox.Show("El primer apellido es obligatorio.", "Campo requerido", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtFirstLastName.Focus();
                return;
            }

            if (!EsNombreValido(_txtFirstLastName.Text))
            {
                MessageBox.Show("El primer apellido solo debe contener letras.", "Apellido inválido", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtFirstLastName.Focus();
                return;
            }

            // Validar segundo apellido
            if (string.IsNullOrWhiteSpace(_txtSecondLastName.Text))
            {
                MessageBox.Show("El segundo apellido es obligatorio.", "Campo requerido", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtSecondLastName.Focus();
                return;
            }

            if (!EsNombreValido(_txtSecondLastName.Text))
            {
                MessageBox.Show("El segundo apellido solo debe contener letras.", "Apellido inválido", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtSecondLastName.Focus();
                return;
            }

            // Validar cédula
            if (string.IsNullOrWhiteSpace(_txtNationalId.Text))
            {
                MessageBox.Show("La cédula es obligatoria.", "Campo requerido", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtNationalId.Focus();
                return;
            }

            // Verificar cédula duplicada
            if (_familyTree.GetAllMembers().Any(p => p.NationalId == _txtNationalId.Text.Trim()))
            {
                MessageBox.Show("Ya existe una persona con esta cédula.", "Cédula duplicada", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtNationalId.Focus();
                return;
            }

            // Validar fecha de nacimiento
            if (_dtpBirthDate.Value > DateTime.Today)
            {
                MessageBox.Show("La fecha de nacimiento no puede ser posterior a hoy.", 
                    "Fecha inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validar fecha de defunción
            if (_chkDeceased.Checked)
            {
                if (_dtpDeathDate.Value <= _dtpBirthDate.Value)
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
            }

            // Validar coordenadas
            if (!TryParseCoordinate(_txtLatitude.Text, out double lat))
            {
                MessageBox.Show("La latitud debe ser un número válido. Use coma (,) como separador decimal.", 
                    "Coordenada inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtLatitude.Focus();
                return;
            }

            if (!TryParseCoordinate(_txtLongitude.Text, out double lng))
            {
                MessageBox.Show("La longitud debe ser un número válido. Use coma (,) como separador decimal.", 
                    "Coordenada inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtLongitude.Focus();
                return;
            }

            // Validar dirección
            if (string.IsNullOrWhiteSpace(_txtAddress.Text))
            {
                MessageBox.Show("La dirección es obligatoria.", "Campo requerido", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtAddress.Focus();
                return;
            }

            // Validar URL de foto
            if (string.IsNullOrWhiteSpace(_txtPhotoUrl.Text))
            {
                MessageBox.Show("La URL de la foto es obligatoria.", "Campo requerido", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtPhotoUrl.Focus();
                return;
            }

            // Validar que tenga al menos un padre
            if (_cmbParent1.SelectedIndex == 0 && _cmbParent2.SelectedIndex == 0)
            {
                MessageBox.Show("Debe seleccionar al menos un padre/madre.", "Padres requeridos", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _cmbParent1.Focus();
                return;
            }

            try
            {
                var person = new Person
                {
                    FirstName = _txtFirstName.Text.Trim(),
                    SecondName = _txtSecondName.Text.Trim(),
                    FirstLastName = _txtFirstLastName.Text.Trim(),
                    SecondLastName = _txtSecondLastName.Text.Trim(),
                    NationalId = _txtNationalId.Text.Trim(),
                    BirthDate = _dtpBirthDate.Value,
                    IsAlive = !_chkDeceased.Checked,
                    DeathDate = _chkDeceased.Checked ? _dtpDeathDate.Value : (DateTime?)null,
                    PhotoUrl = _txtPhotoUrl.Text.Trim(),
                    Latitude = lat,
                    Longitude = lng,
                    Address = _txtAddress.Text.Trim()
                };

                person.CalculateAge();
                _familyTree.AddPerson(person);

                // Agregar relaciones
                if (_cmbParent1.SelectedIndex > 0)
                {
                    var parent1 = _cmbParent1.SelectedItem as ComboBoxItem;
                    if (parent1 != null)
                        _familyTree.AddParentRelation(person.Id, parent1.Value);
                }

                if (_cmbParent2.SelectedIndex > 0)
                {
                    var parent2 = _cmbParent2.SelectedItem as ComboBoxItem;
                    if (parent2 != null)
                        _familyTree.AddParentRelation(person.Id, parent2.Value);
                }

                if (_cmbSpouse.SelectedIndex > 0)
                {
                    var spouse = _cmbSpouse.SelectedItem as ComboBoxItem;
                    if (spouse != null)
                        _familyTree.AddSpouseRelation(person.Id, spouse.Value);
                }

                DataLoader.SaveToJson(_familyTree);

                MessageBox.Show("Persona agregada y guardada correctamente.", "Éxito", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool EsNombreValido(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return false;
            
            foreach (char c in nombre)
            {
                if (!char.IsLetter(c) && c != ' ' && c != 'á' && c != 'é' && c != 'í' && 
                    c != 'ó' && c != 'ú' && c != 'Á' && c != 'É' && c != 'Í' && c != 'Ó' && c != 'Ú' &&
                    c != 'ñ' && c != 'Ñ')
                    return false;
            }
            return true;
        }

        private bool TryParseCoordinate(string text, out double value)
        {
            text = text.Replace('.', ',');
            return double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        private class ComboBoxItem
        {
            public string Value { get; set; }
            public string Display { get; set; }

            public ComboBoxItem(string value, string display)
            {
                Value = value;
                Display = display;
            }

            public override string ToString()
            {
                return Display;
            }
        }
    }
}