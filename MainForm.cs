using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace WorldMapZoom
{
    public partial class MainForm : Form
    {
        private WebView2 _webView;
        private Panel _sidePanel;
        private Button _btnAddPerson;
        private Button _btnDeletePerson;
        private Button _btnViewTree;
        private Button _btnStatistics;
        private Button _btnMarkDeceased;
        private Label _lblTitle;
        private Label _lblInfo;
        private FamilyTree _familyTree;

        public MainForm()
        {
            InitializeComponent();
            _familyTree = new FamilyTree();
            InitializeUI();
            InitializeWebView();
            LoadInitialData();
        }

        private void LoadInitialData()
        {
            try
            {
                string defaultPath = DataLoader.GetDefaultJsonPath();
                if (File.Exists(defaultPath))
                {
                    DataLoader.LoadFromJson(defaultPath, _familyTree);
                    _lblInfo.Text = $"Cargados: {_familyTree.GetAllMembers().Count} miembros";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos iniciales: {ex.Message}", "Advertencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void InitializeUI()
        {
            Text = "Árbol Genealógico - Mapa Mundial";
            Width = 1400;
            Height = 900;
            StartPosition = FormStartPosition.CenterScreen;

            _sidePanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 250,
                BackColor = Color.FromArgb(45, 45, 48)
            };
            Controls.Add(_sidePanel);

            _lblTitle = new Label
            {
                Text = "ÁRBOL GENEALÓGICO",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 60,
                Dock = DockStyle.Top
            };
            _sidePanel.Controls.Add(_lblTitle);

            _lblInfo = new Label
            {
                Text = "Sin datos cargados",
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 9),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 30,
                Top = 60,
                Width = 250
            };
            _sidePanel.Controls.Add(_lblInfo);

            int yPosition = 100;

            _btnAddPerson = CreateStyledButton("➕ Agregar Persona", yPosition, Color.FromArgb(46, 204, 113));
            _btnAddPerson.Click += BtnAddPerson_Click;
            _sidePanel.Controls.Add(_btnAddPerson);
            yPosition += 60;

            _btnDeletePerson = CreateStyledButton("🗑️ Eliminar Persona", yPosition, Color.FromArgb(192, 57, 43));
            _btnDeletePerson.Click += BtnDeletePerson_Click;
            _sidePanel.Controls.Add(_btnDeletePerson);
            yPosition += 60;

            _btnMarkDeceased = CreateStyledButton("💀 Marcar Fallecido", yPosition, Color.FromArgb(149, 165, 166));
            _btnMarkDeceased.Click += BtnMarkDeceased_Click;
            _sidePanel.Controls.Add(_btnMarkDeceased);
            yPosition += 60;

            _btnViewTree = CreateStyledButton("🌳 Ver Árbol Familiar", yPosition, Color.FromArgb(52, 152, 219));
            _btnViewTree.Click += BtnViewTree_Click;
            _sidePanel.Controls.Add(_btnViewTree);
            yPosition += 60;

            _btnStatistics = CreateStyledButton("📊 Estadísticas", yPosition, Color.FromArgb(155, 89, 182));
            _btnStatistics.Click += BtnStatistics_Click;
            _sidePanel.Controls.Add(_btnStatistics);

            _webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(_webView);
        }

        private Button CreateStyledButton(string text, int yPosition, Color? customColor = null)
        {
            var button = new Button
            {
                Text = text,
                Location = new Point(20, yPosition),
                Size = new Size(210, 45),
                FlatStyle = FlatStyle.Flat,
                BackColor = customColor ?? Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;

            // Efecto hover
            Color hoverColor = ControlPaint.Light(customColor ?? Color.FromArgb(0, 122, 204), 0.2f);
            button.MouseEnter += (s, e) => button.BackColor = hoverColor;
            button.MouseLeave += (s, e) => button.BackColor = customColor ?? Color.FromArgb(0, 122, 204);

            return button;
        }

        private async void InitializeWebView()
        {
            try
            {
                await _webView.EnsureCoreWebView2Async(null);

                if (_webView.CoreWebView2 != null)
                {
                    // Permitir acceso a archivos locales
                    _webView.CoreWebView2.Settings.AreHostObjectsAllowed = true;
                    _webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                    _webView.CoreWebView2.Settings.AreDevToolsEnabled = true; // Habilitar temporalmente para debug
                    
                    _webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;
                    RefreshMap();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar WebView2: {ex.Message}\n\nAsegúrate de tener WebView2 Runtime instalado.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WebView_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string message = e.TryGetWebMessageAsString();

                if (message.StartsWith("person:"))
                {
                    string personId = message.Substring(7);
                    ShowDistancesForPerson(personId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error procesando mensaje: {ex.Message}");
            }
        }

        private void ShowDistancesForPerson(string personId)
        {
            var graph = _familyTree.GetGraph();
            var person = graph.GetPerson(personId);

            if (person == null) return;

            var distances = graph.GetDistancesFrom(personId);

            var script = new StringBuilder();
            script.AppendLine("if (window.distanceLines) {");
            script.AppendLine("  window.distanceLines.forEach(line => map.removeLayer(line));");
            script.AppendLine("  window.distanceLines = [];");
            script.AppendLine("} else {");
            script.AppendLine("  window.distanceLines = [];");
            script.AppendLine("}");

            foreach (var kvp in distances)
            {
                var otherPerson = graph.GetPerson(kvp.Key);
                if (otherPerson != null)
                {
                    // Usar InvariantCulture para garantizar formato con punto decimal
                    var lat1 = person.Latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                    var lng1 = person.Longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                    var lat2 = otherPerson.Latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                    var lng2 = otherPerson.Longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                    
                    script.AppendLine($"console.log('Creating line from [{lat1}, {lng1}] to [{lat2}, {lng2}]');");
                    script.AppendLine($"var line = L.polyline([[{lat1}, {lng1}], [{lat2}, {lng2}]], {{");
                    script.AppendLine("  color: 'black',");
                    script.AppendLine("  weight: 3,");
                    script.AppendLine("  opacity: 0.8");
                    script.AppendLine("}).addTo(map);");
                    script.AppendLine("window.distanceLines.push(line);");
                    
                    // Calcular el punto medio de la línea para mostrar la distancia
                    script.AppendLine($"var midLat = ({lat1} + {lat2}) / 2;");
                    script.AppendLine($"var midLng = ({lng1} + {lng2}) / 2;");
                    
                    // Crear un marcador de texto en el punto medio
                    var distanceText = kvp.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                    script.AppendLine($"var distanceIcon = L.divIcon({{");
                    script.AppendLine($"  html: '<div style=\"min-width: 60px; text-align: center; font-size: 12px; font-weight: bold; color: black; text-shadow: 1px 1px 2px white, -1px -1px 2px white, 1px -1px 2px white, -1px 1px 2px white;\">{distanceText} km</div>',");
                    script.AppendLine("  className: 'distance-label',");
                    script.AppendLine("  iconSize: [null, null],");
                    script.AppendLine("  iconAnchor: [null, null]");
                    script.AppendLine("});");
                    script.AppendLine($"var distanceMarker = L.marker([midLat, midLng], {{icon: distanceIcon}}).addTo(map);");
                    script.AppendLine("window.distanceLines.push(distanceMarker);");
                }
            }

            script.AppendLine($"console.log('Total lines created:', window.distanceLines.length);");
            _webView.CoreWebView2?.ExecuteScriptAsync(script.ToString());
        }

        private void BtnAddPerson_Click(object sender, EventArgs e)
        {
            var addForm = new AddPersonForm(_familyTree);
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                RefreshMap();
                _lblInfo.Text = $"Cargados: {_familyTree.GetAllMembers().Count} miembros";
            }
        }

        private void BtnDeletePerson_Click(object sender, EventArgs e)
        {
            if (_familyTree.GetAllMembers().Count == 0)
            {
                MessageBox.Show("No hay personas para eliminar en el árbol genealógico.",
                    "Árbol vacío", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var deleteForm = new DeletePersonForm(_familyTree);
            if (deleteForm.ShowDialog() == DialogResult.OK)
            {
                RefreshMap();
                _lblInfo.Text = $"Cargados: {_familyTree.GetAllMembers().Count} miembros";
            }
        }

        private void BtnMarkDeceased_Click(object sender, EventArgs e)
        {
            var markDeceasedForm = new MarkDeceasedForm(_familyTree);
            if (markDeceasedForm.ShowDialog() == DialogResult.OK)
            {
                RefreshMap();
            }
        }

        private void BtnViewTree_Click(object sender, EventArgs e)
        {
            if (_familyTree.GetAllMembers().Count == 0)
            {
                MessageBox.Show("No hay miembros en el árbol genealógico.", "Árbol vacío",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var treeForm = new FamilyTreeForm(_familyTree);
            treeForm.ShowDialog();
        }

        private void BtnStatistics_Click(object sender, EventArgs e)
        {
            if (_familyTree.GetAllMembers().Count < 2)
            {
                MessageBox.Show("Se necesitan al menos 2 personas para calcular estadísticas.",
                    "Datos insuficientes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var statsForm = new StatisticsForm(_familyTree);
            statsForm.ShowDialog();
        }

        public void RefreshMap()
        {
            if (_webView.CoreWebView2 != null)
            {
                _webView.CoreWebView2.NavigateToString(BuildMapHtml());
            }
        }

        private string BuildMapHtml()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!doctype html><html><head><meta charset='utf-8'/>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'/>");
            sb.AppendLine("<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>");
            sb.AppendLine("<style>");
            sb.AppendLine("html,body,#map{height:100%;margin:0;padding:0;}");
            sb.AppendLine(".user-photo{border-radius:50%;border:3px solid white;box-shadow:0 2px 8px rgba(0,0,0,0.3);object-fit:cover;cursor:pointer;transition:all 0.3s;}");
            sb.AppendLine(".user-photo.deceased{opacity:0.5;border-color:#666;}");
            sb.AppendLine(".user-photo:hover{border-color:#007acc;transform:scale(1.15);}");
            sb.AppendLine("</style>");
            sb.AppendLine("</head><body><div id='map'></div>");
            sb.AppendLine("<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>");
            sb.AppendLine("<script>");

            sb.AppendLine("var users = " + GetUsersJsonFromTree() + ";");

            sb.AppendLine("var map = L.map('map').setView([9.935, -84.091], 8);");
            sb.AppendLine("L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {");
            sb.AppendLine("  maxZoom: 19, attribution: '&copy; OpenStreetMap contributors'");
            sb.AppendLine("}).addTo(map);");

            sb.AppendLine("function getPhotoSize(zoom) {");
            sb.AppendLine("  if (zoom < 6) return 20;");
            sb.AppendLine("  if (zoom < 8) return 30;");
            sb.AppendLine("  if (zoom < 10) return 40;");
            sb.AppendLine("  if (zoom < 12) return 50;");
            sb.AppendLine("  if (zoom < 14) return 60;");
            sb.AppendLine("  return 80;");
            sb.AppendLine("}");

            sb.AppendLine("var markers = [];");
            sb.AppendLine("var selectedPersonId = null;");
            sb.AppendLine("users.forEach(function(user) {");
            sb.AppendLine("  var size = getPhotoSize(map.getZoom());");
            sb.AppendLine("  var icon = L.divIcon({");
            sb.AppendLine("    html: '<img src=\"' + user.photo_url + '\" class=\"user-photo' + (user.alive ? '' : ' deceased') + '\" width=\"' + size + '\" height=\"' + size + '\" title=\"Click para ver distancias\">',");
            sb.AppendLine("    iconSize: [size, size],");
            sb.AppendLine("    iconAnchor: [size/2, size/2],");
            sb.AppendLine("    className: ''");
            sb.AppendLine("  });");
            sb.AppendLine("  var popupContent = '<b>' + user.name + '</b><br>' + user.address + '<br>Cédula: ' + user.national_id + '<br>';");
            sb.AppendLine("  if (user.alive) { popupContent += 'Edad: ' + user.age + ' años'; }");
            sb.AppendLine("  else { popupContent += '† ' + user.age + ' años'; }");
            sb.AppendLine("  var marker = L.marker([user.lat, user.lng], {icon: icon});");
            sb.AppendLine("  marker.bindPopup(popupContent);");
            sb.AppendLine("  marker.on('click', function() { selectPerson(user.id); });");
            sb.AppendLine("  marker.addTo(map);");
            sb.AppendLine("  markers.push({marker: marker, user: user});");
            sb.AppendLine("});");

            sb.AppendLine("map.on('zoomend', function() {");
            sb.AppendLine("  var size = getPhotoSize(map.getZoom());");
            sb.AppendLine("  markers.forEach(function(m) {");
            sb.AppendLine("    var icon = L.divIcon({");
            sb.AppendLine("      html: '<img src=\"' + m.user.photo_url + '\" class=\"user-photo' + (m.user.alive ? '' : ' deceased') + '\" width=\"' + size + '\" height=\"' + size + '\" title=\"Click para ver distancias\">',");
            sb.AppendLine("      iconSize: [size, size],");
            sb.AppendLine("      iconAnchor: [size/2, size/2],");
            sb.AppendLine("      className: ''");
            sb.AppendLine("    });");
            sb.AppendLine("    m.marker.setIcon(icon);");
            sb.AppendLine("    // Re-attach click event after icon update");
            sb.AppendLine("    m.marker.off('click');");
            sb.AppendLine("    m.marker.on('click', function() { selectPerson(m.user.id); });");
            sb.AppendLine("  });");
            sb.AppendLine("});");

            sb.AppendLine("function selectPerson(personId) {");
            sb.AppendLine("  console.log('selectPerson called with:', personId);");
            sb.AppendLine("  if (selectedPersonId === personId) {");
            sb.AppendLine("    if (window.distanceLines) {");
            sb.AppendLine("      window.distanceLines.forEach(line => map.removeLayer(line));");
            sb.AppendLine("      window.distanceLines = [];");
            sb.AppendLine("    }");
            sb.AppendLine("    selectedPersonId = null;");
            sb.AppendLine("  } else {");
            sb.AppendLine("    selectedPersonId = personId;");
            sb.AppendLine("    console.log('Sending message:', 'person:' + personId);");
            sb.AppendLine("    window.chrome.webview.postMessage('person:' + personId);");
            sb.AppendLine("  }");
            sb.AppendLine("}");

            sb.AppendLine("</script></body></html>");
            return sb.ToString();
        }

        private string GetUsersJsonFromTree()
        {
            var members = _familyTree.GetAllMembers();
            if (members.Count == 0)
                return "[]";

            var sb = new StringBuilder();
            sb.Append("[");

            for (int i = 0; i < members.Count; i++)
            {
                var person = members[i];
                if (i > 0) sb.Append(",");

                sb.Append("{");
                sb.AppendFormat("\"id\":\"{0}\",", EscapeJson(person.Id));
                sb.AppendFormat("\"name\":\"{0}\",", EscapeJson(person.FullName));
                sb.AppendFormat("\"national_id\":\"{0}\",", EscapeJson(person.NationalId));
                sb.AppendFormat("\"photo_url\":\"{0}\",", EscapeJson(GetAbsolutePhotoUrl(person.PhotoUrl)));
                sb.AppendFormat("\"lat\":{0},", person.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture));
                sb.AppendFormat("\"lng\":{0},", person.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture));
                sb.AppendFormat("\"address\":\"{0}\",", EscapeJson(person.Address));
                sb.AppendFormat("\"age\":{0},", person.Age);
                sb.AppendFormat("\"alive\":{0}", person.IsAlive.ToString().ToLower());
                sb.Append("}");
            }

            sb.Append("]");
            return sb.ToString();
        }

        private string GetAbsolutePhotoUrl(string photoUrl)
        {
            if (string.IsNullOrEmpty(photoUrl))
                return photoUrl;

            // Si la URL empieza con "Images/", convertirla a data URL base64
            if (photoUrl.StartsWith("Images/"))
            {
                try
                {
                    string absolutePath = System.IO.Path.Combine(Environment.CurrentDirectory, photoUrl);
                    if (File.Exists(absolutePath))
                    {
                        byte[] imageBytes = File.ReadAllBytes(absolutePath);
                        string base64String = Convert.ToBase64String(imageBytes);
                        string extension = Path.GetExtension(absolutePath).ToLower();
                        string mimeType = GetMimeType(extension);
                        return $"data:{mimeType};base64,{base64String}";
                    }
                }
                catch
                {
                    // Si hay error, devolver una imagen placeholder o la URL original
                }
            }

            // Si es una URL completa (http/https), devolverla tal como está
            return photoUrl;
        }

        private string GetMimeType(string extension)
        {
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".bmp":
                    return "image/bmp";
                default:
                    return "image/jpeg";
            }
        }

        private string EscapeJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace("\\", "\\\\")
                      .Replace("\"", "\\\"")
                      .Replace("\n", "\\n")
                      .Replace("\r", "\\r");
        }
    }
}