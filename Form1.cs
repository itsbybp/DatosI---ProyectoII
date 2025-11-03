using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace WorldMapZoom
{
    public partial class Form1 : Form
    {
        private WebView2 _web;

        public Form1()
        {
            InitializeComponent();
            Text = "World Map (Leaflet + WebView2)";
            Width = 1200;
            Height = 800;

            _web = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(_web);

            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                await _web.EnsureCoreWebView2Async(null);
                
                if (_web.CoreWebView2 != null)
                {
                    _web.CoreWebView2.NavigateToString(BuildHtml());
                }
                else
                {
                    MessageBox.Show("WebView2 no se inicializó correctamente.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar WebView2: {ex.Message}\n\nAsegúrate de tener WebView2 Runtime instalado.");
            }
        }

        private string BuildHtml()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!doctype html><html><head><meta charset='utf-8'/>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'/>");
            sb.AppendLine("<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>");
            sb.AppendLine("<style>");
            sb.AppendLine("html,body,#map{height:100%;margin:0;padding:0;}");
            sb.AppendLine(".user-photo{border-radius:50%;border:3px solid white;box-shadow:0 2px 8px rgba(0,0,0,0.3);object-fit:cover;}");
            sb.AppendLine(".user-photo.deceased{opacity:0.5;border-color:#999;}");
            sb.AppendLine("</style>");
            sb.AppendLine("</head><body><div id='map'></div>");
            sb.AppendLine("<script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>");
            sb.AppendLine("<script>");
            
            // Datos de usuarios
            sb.AppendLine("var users = " + GetUsersJson() + ";");
            
            sb.AppendLine("var map = L.map('map').setView([9.935, -84.091], 8);");
            sb.AppendLine("L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {");
            sb.AppendLine("  maxZoom: 19, attribution: '&copy; OpenStreetMap contributors'");
            sb.AppendLine("}).addTo(map);");
            
            // Función para calcular el tamaño según el zoom
            sb.AppendLine("function getPhotoSize(zoom) {");
            sb.AppendLine("  if (zoom < 6) return 20;");
            sb.AppendLine("  if (zoom < 8) return 30;");
            sb.AppendLine("  if (zoom < 10) return 40;");
            sb.AppendLine("  if (zoom < 12) return 50;");
            sb.AppendLine("  if (zoom < 14) return 60;");
            sb.AppendLine("  return 80;");
            sb.AppendLine("}");
            
            // Agregar marcadores
            sb.AppendLine("var markers = [];");
            sb.AppendLine("users.forEach(function(user) {");
            sb.AppendLine("  var size = getPhotoSize(map.getZoom());");
            sb.AppendLine("  var icon = L.divIcon({");
            sb.AppendLine("    html: '<img src=\"' + user.photo_url + '\" class=\"user-photo' + (user.alive ? '' : ' deceased') + '\" width=\"' + size + '\" height=\"' + size + '\">',");
            sb.AppendLine("    iconSize: [size, size],");
            sb.AppendLine("    iconAnchor: [size/2, size/2],");
            sb.AppendLine("    className: ''");
            sb.AppendLine("  });");
            sb.AppendLine("  var marker = L.marker([user.lat, user.lng], {icon: icon})");
            sb.AppendLine("    .bindPopup('<b>' + user.name + '</b><br>' + user.address + '<br>Edad: ' + user.age)");
            sb.AppendLine("    .addTo(map);");
            sb.AppendLine("  markers.push({marker: marker, user: user});");
            sb.AppendLine("});");
            
            // Actualizar tamaño al hacer zoom
            sb.AppendLine("map.on('zoomend', function() {");
            sb.AppendLine("  var size = getPhotoSize(map.getZoom());");
            sb.AppendLine("  markers.forEach(function(m) {");
            sb.AppendLine("    var icon = L.divIcon({");
            sb.AppendLine("      html: '<img src=\"' + m.user.photo_url + '\" class=\"user-photo' + (m.user.alive ? '' : ' deceased') + '\" width=\"' + size + '\" height=\"' + size + '\">',");
            sb.AppendLine("      iconSize: [size, size],");
            sb.AppendLine("      iconAnchor: [size/2, size/2],");
            sb.AppendLine("      className: ''");
            sb.AppendLine("    });");
            sb.AppendLine("    m.marker.setIcon(icon);");
            sb.AppendLine("  });");
            sb.AppendLine("});");
            
            sb.AppendLine("</script></body></html>");
            return sb.ToString();
        }

        private string GetUsersJson()
        {
            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "usuarios.json");
            if (!File.Exists(jsonPath))
            {
                MessageBox.Show($"No se encontró el archivo usuarios.json en: {jsonPath}");
                return "[]";
            }
            
            try
            {
                var json = File.ReadAllText(jsonPath, Encoding.UTF8);
                var doc = JsonDocument.Parse(json);
                var people = doc.RootElement.GetProperty("people");
                
                var sb = new StringBuilder();
                sb.Append("[");
                bool first = true;
                foreach (var person in people.EnumerateArray())
                {
                    if (!first) sb.Append(",");
                    first = false;
                    
                    var location = person.GetProperty("location");
                    sb.Append("{");
                    sb.AppendFormat("\"name\":\"{0}\",", EscapeJson(person.GetProperty("name").GetString()));
                    sb.AppendFormat("\"photo_url\":\"{0}\",", person.GetProperty("photo_url").GetString());
                    sb.AppendFormat("\"lat\":{0},", location.GetProperty("lat").GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture));
                    sb.AppendFormat("\"lng\":{0},", location.GetProperty("lng").GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture));
                    sb.AppendFormat("\"address\":\"{0}\",", EscapeJson(location.GetProperty("address").GetString()));
                    sb.AppendFormat("\"age\":{0},", person.GetProperty("age").GetInt32());
                    sb.AppendFormat("\"alive\":{0}", person.GetProperty("alive").GetBoolean().ToString().ToLower());
                    sb.Append("}");
                }
                sb.Append("]");
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al leer usuarios.json: {ex.Message}");
                return "[]";
            }
        }

        private string EscapeJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}