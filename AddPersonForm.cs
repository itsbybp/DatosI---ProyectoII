using System;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

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
        private Button _btnSelectPhoto;
        private Label _lblSelectedPhoto;
        private string _selectedPhotoPath;
        private TextBox _txtLatitude;
        private TextBox _txtLongitude;
        private TextBox _txtAddress;
        private ComboBox _cmbParent1;
        private ComboBox _cmbParent2;
        private ComboBox _cmbSpouse;
        private Button _btnSave;
        private Button _btnCancel;
        private WebView2 _webView;
        private bool _mapInitialized = false;

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
            Width = 650;
            Height = 700;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(650, 600);
            BackColor = Color.White;
            AutoScroll = true;

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

            AddLabel("Foto *:", 20, yPos, labelWidth);
            _btnSelectPhoto = new Button
            {
                Location = new Point(textBoxX, yPos),
                Width = 120,
                Height = 23,
                Text = "Seleccionar Foto",
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnSelectPhoto.FlatAppearance.BorderSize = 0;
            _btnSelectPhoto.Click += BtnSelectPhoto_Click;
            Controls.Add(_btnSelectPhoto);

            _lblSelectedPhoto = new Label
            {
                Location = new Point(textBoxX + 130, yPos + 3),
                Width = textBoxWidth - 130,
                Height = 20,
                Text = "Ningún archivo seleccionado",
                ForeColor = Color.Gray
            };
            Controls.Add(_lblSelectedPhoto);
            yPos += 40;

            AddLabel("Ubicación (haga clic en el mapa) *:", 20, yPos, labelWidth + 150);
            yPos += 30;
            
            AddLabel("Latitud:", 20, yPos, labelWidth);
            _txtLatitude = AddTextBox(textBoxX, yPos, 150);
            _txtLatitude.Text = "9.935";
            _txtLatitude.ReadOnly = true;
            _txtLatitude.BackColor = Color.LightGray;
            
            AddLabel("Longitud:", textBoxX + 160, yPos, 80);
            _txtLongitude = AddTextBox(textBoxX + 240, yPos, 150);
            _txtLongitude.Text = "-84.091";
            _txtLongitude.ReadOnly = true;
            _txtLongitude.BackColor = Color.LightGray;
            yPos += 40;
            
            // Agregar WebView2 para el mapa
            _webView = new WebView2
            {
                Location = new Point(20, yPos),
                Size = new Size(textBoxWidth + labelWidth + 10, 250),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            Controls.Add(_webView);
            InitializeMapAsync();
            yPos += 260;

            AddLabel("Dirección *:", 20, yPos, labelWidth);
            _txtAddress = AddTextBox(textBoxX, yPos, textBoxWidth);
            yPos += 40;

            AddLabel("Padre/Madre 1:", 20, yPos, labelWidth);
            _cmbParent1 = new ComboBox
            {
                Location = new Point(textBoxX, yPos),
                Width = textBoxWidth,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbParent1.SelectedIndexChanged += CmbParent_SelectedIndexChanged;
            Controls.Add(_cmbParent1);
            yPos += 40;

            AddLabel("Padre/Madre 2:", 20, yPos, labelWidth);
            _cmbParent2 = new ComboBox
            {
                Location = new Point(textBoxX, yPos),
                Width = textBoxWidth,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbParent2.SelectedIndexChanged += CmbParent_SelectedIndexChanged;
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
            yPos += 60;

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

        private void CmbParent_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Validar que no se seleccione la misma persona en ambos padres
            if (_cmbParent1.SelectedIndex > 0 && _cmbParent2.SelectedIndex > 0)
            {
                var parent1 = _cmbParent1.SelectedItem as ComboBoxItem;
                var parent2 = _cmbParent2.SelectedItem as ComboBoxItem;
                
                if (parent1 != null && parent2 != null && parent1.Value == parent2.Value)
                {
                    MessageBox.Show("No puede seleccionar la misma persona como Padre/Madre 1 y Padre/Madre 2.", 
                        "Selección inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    
                    // Resetear el ComboBox que acaba de cambiar
                    var changedCombo = sender as ComboBox;
                    if (changedCombo != null)
                    {
                        changedCombo.SelectedIndex = 0;
                    }
                }
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

            // Validar formato de cédula
            if (!EsCedulaValidaFormato(_txtNationalId.Text))
            {
                MessageBox.Show("La cédula debe tener el formato #-####-####, donde # es un número del 0 al 9.\nEjemplo: 1-2345-6789", 
                    "Formato de cédula inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            // Validar foto
            if (string.IsNullOrWhiteSpace(_selectedPhotoPath))
            {
                MessageBox.Show("Debe seleccionar una foto.", "Campo requerido", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _btnSelectPhoto.Focus();
                return;
            }

            // Validar que tenga al menos un padre O un cónyuge (solo si el árbol no está vacío)
            bool arbolVacio = _familyTree.GetAllMembers().Count == 0;
            bool tieneAlMenosUnPadre = _cmbParent1.SelectedIndex > 0 || _cmbParent2.SelectedIndex > 0;
            bool tieneConyugue = _cmbSpouse.SelectedIndex > 0;
            
            if (!arbolVacio && !tieneAlMenosUnPadre && !tieneConyugue)
            {
                MessageBox.Show("Debe seleccionar al menos un padre/madre o un cónyuge.", 
                    "Relación familiar requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _cmbParent1.Focus();
                return;
            }

            // Validar que los padres no sean la misma persona
            if (_cmbParent1.SelectedIndex > 0 && _cmbParent2.SelectedIndex > 0)
            {
                var parent1 = _cmbParent1.SelectedItem as ComboBoxItem;
                var parent2 = _cmbParent2.SelectedItem as ComboBoxItem;
                if (parent1 != null && parent2 != null && parent1.Value == parent2.Value)
                {
                    MessageBox.Show("Los dos padres no pueden ser la misma persona.", "Padres duplicados", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _cmbParent2.Focus();
                    return;
                }
            }

            try
            {
                // Copiar imagen a la carpeta Images con el nombre de la cédula
                string imagesFolder = Path.Combine(Environment.CurrentDirectory, "Images");
                if (!Directory.Exists(imagesFolder))
                {
                    Directory.CreateDirectory(imagesFolder);
                }

                string fileExtension = Path.GetExtension(_selectedPhotoPath);
                string targetFileName = _txtNationalId.Text.Trim() + fileExtension;
                string targetPath = Path.Combine(imagesFolder, targetFileName);

                File.Copy(_selectedPhotoPath, targetPath, true);

                var person = new Person
                {
                    FirstName = _txtFirstName.Text.Trim(),
                    SecondName = _txtSecondName.Text.Trim(),
                    FirstLastName = _txtFirstLastName.Text.Trim(),
                    SecondLastName = _txtSecondLastName.Text.Trim(),
                    NationalId = _txtNationalId.Text.Trim(),
                    BirthDate = _dtpBirthDate.Value,
                    IsAlive = true,
                    DeathDate = null,
                    PhotoUrl = $"Images/{targetFileName}",
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

        private bool EsCedulaValidaFormato(string cedula)
        {
            if (string.IsNullOrWhiteSpace(cedula))
                return false;

            // Patrón regex para formato #-####-####
            // Donde # es un dígito del 0 al 9
            var patron = @"^\d-\d{4}-\d{4}$";
            
            return System.Text.RegularExpressions.Regex.IsMatch(cedula.Trim(), patron);
        }

        private async void InitializeMapAsync()
        {
            try
            {
                await _webView.EnsureCoreWebView2Async(null);
                
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                _webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;
                
                string html = BuildMapHtml();
                _webView.NavigateToString(html);
                _mapInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error inicializando mapa: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string message = e.TryGetWebMessageAsString();
                
                if (message.StartsWith("location:"))
                {
                    string coords = message.Substring(9);
                    string[] parts = coords.Split(',');
                    
                    if (parts.Length == 2)
                    {
                        _txtLatitude.Text = parts[0];
                        _txtLongitude.Text = parts[1];
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error procesando ubicación: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string BuildMapHtml()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8' />");
            sb.AppendLine("<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />");
            sb.AppendLine("<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { margin: 0; padding: 0; }");
            sb.AppendLine("#map { height: 100vh; cursor: crosshair; }");
            sb.AppendLine(".selected-marker { z-index: 1000 !important; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div id='map'></div>");
            sb.AppendLine("<script>");
            
            var currentLat = _txtLatitude.Text.Replace(",", ".");
            var currentLng = _txtLongitude.Text.Replace(",", ".");
            
            sb.AppendLine($"var map = L.map('map').setView([{currentLat}, {currentLng}], 10);");
            sb.AppendLine("L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {");
            sb.AppendLine("  maxZoom: 19,");
            sb.AppendLine("  attribution: '© OpenStreetMap'");
            sb.AppendLine("}).addTo(map);");
            
            sb.AppendLine("var marker = null;");
            sb.AppendLine($"marker = L.marker([{currentLat}, {currentLng}]).addTo(map);");
            sb.AppendLine("marker.bindPopup('Ubicación seleccionada').openPopup();");
            
            sb.AppendLine("map.on('click', function(e) {");
            sb.AppendLine("  var lat = e.latlng.lat.toFixed(6);");
            sb.AppendLine("  var lng = e.latlng.lng.toFixed(6);");
            sb.AppendLine("  ");
            sb.AppendLine("  if (marker) {");
            sb.AppendLine("    map.removeLayer(marker);");
            sb.AppendLine("  }");
            sb.AppendLine("  ");
            sb.AppendLine("  marker = L.marker([lat, lng]).addTo(map);");
            sb.AppendLine("  marker.bindPopup('Ubicación seleccionada<br>Lat: ' + lat + '<br>Lng: ' + lng).openPopup();");
            sb.AppendLine("  ");
            sb.AppendLine("  window.chrome.webview.postMessage('location:' + lat + ',' + lng);");
            sb.AppendLine("});");
            
            sb.AppendLine("</script>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            return sb.ToString();
        }

        private void BtnSelectPhoto_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Archivos de imagen|*.jpg;*.jpeg;*.png;*.gif;*.bmp|Todos los archivos|*.*";
                openFileDialog.Title = "Seleccionar foto para la persona";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _selectedPhotoPath = openFileDialog.FileName;
                    _lblSelectedPhoto.Text = System.IO.Path.GetFileName(_selectedPhotoPath);
                    _lblSelectedPhoto.ForeColor = Color.Black;
                }
            }
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