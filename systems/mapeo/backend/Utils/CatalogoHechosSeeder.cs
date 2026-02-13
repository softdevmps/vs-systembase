using Backend.Data;
using Microsoft.Data.SqlClient;

namespace Backend.Utils
{
    public static class CatalogoHechosSeeder
    {
        private sealed class SeedItem
        {
            public SeedItem(string codigo, string nombre, string categoria, string subcategoria, string palabrasClave)
            {
                Codigo = codigo;
                Nombre = nombre;
                Categoria = categoria;
                Subcategoria = subcategoria;
                PalabrasClave = palabrasClave;
            }

            public string Codigo { get; }
            public string Nombre { get; }
            public string Categoria { get; }
            public string Subcategoria { get; }
            public string PalabrasClave { get; }
        }

        private static readonly List<SeedItem> Items = new()
        {
            new("ROB_MANO_ARMADA", "Robo a mano armada", "Delitos contra la propiedad", "Robo", "arma, pistola, revolver, asalto, amenaza"),
            new("ROB_SIMPLE", "Robo simple", "Delitos contra la propiedad", "Robo", "robo, sustraccion, forzar, ingreso"),
            new("ROB_VEHICULO", "Robo de vehiculo", "Delitos contra la propiedad", "Robo", "auto, coche, vehiculo, sustraido"),
            new("ROB_MOTO", "Robo de moto", "Delitos contra la propiedad", "Robo", "moto, motocicleta, sustraida"),
            new("ROB_BICICLETA", "Robo de bicicleta", "Delitos contra la propiedad", "Robo", "bicicleta, bici, sustraida"),
            new("ROB_COMERCIO", "Robo en comercio", "Delitos contra la propiedad", "Robo", "local, negocio, caja, mercaderia"),
            new("HURTO", "Hurto", "Delitos contra la propiedad", "Hurto", "hurto, sustraccion, sin violencia"),
            new("HURTO_VIVIENDA", "Hurto en vivienda", "Delitos contra la propiedad", "Hurto", "casa, vivienda, domicilio"),
            new("ABIGEATO", "Abigeato", "Delitos contra la propiedad", "Robo", "ganado, animales, campo"),
            new("DANOS", "Danios", "Delitos contra la propiedad", "Danios", "danos, vandalismo, rotura"),
            new("ESTAFA", "Estafa", "Delitos contra la propiedad", "Fraude", "estafa, engaño, fraude"),
            new("EXTORSION", "Extorsion", "Delitos contra la propiedad", "Fraude", "extorsion, amenaza, cobro"),
            new("USURPACION", "Usurpacion", "Delitos contra la propiedad", "Usurpacion", "ocupacion, toma, usurpacion"),
            new("VIOLACION_DOMICILIO", "Violacion de domicilio", "Delitos contra la propiedad", "Intrusion", "intrusion, domicilio, ingreso"),
            new("ROBO_CABLES", "Robo de cables", "Delitos contra la propiedad", "Robo", "cables, cobre, postes"),

            new("AMENAZAS", "Amenazas", "Delitos contra las personas", "Amenazas", "amenaza, intimidacion"),
            new("LESIONES_LEVES", "Lesiones leves", "Delitos contra las personas", "Lesiones", "golpes, lesiones, heridas"),
            new("LESIONES_GRAVES", "Lesiones graves", "Delitos contra las personas", "Lesiones", "grave, hospital, fractura"),
            new("HOMICIDIO", "Homicidio", "Delitos contra las personas", "Homicidio", "muerte, asesinato"),
            new("PRIVACION_LIBERTAD", "Privacion de libertad", "Delitos contra las personas", "Privacion", "secuestro, retencion"),
            new("ABUSO_SEXUAL", "Abuso sexual", "Delitos contra las personas", "Abuso", "abuso, agresion sexual"),
            new("VIOLENCIA_GENERO", "Violencia de genero", "Delitos contra las personas", "Violencia", "violencia, genero, pareja"),
            new("VIOLENCIA_FAMILIAR", "Violencia familiar", "Delitos contra las personas", "Violencia", "familiar, violencia domestica"),
            new("RIÑA", "Rina o pelea", "Delitos contra las personas", "Violencia", "pelea, rina, agresion"),
            new("DESAPARICION", "Persona desaparecida", "Delitos contra las personas", "Desaparicion", "desaparecida, extraviada"),

            new("PORTACION_ARMA", "Portacion de arma", "Armas", "Portacion", "arma, portacion, ilegal"),
            new("TENENCIA_ARMA", "Tenencia ilegal de arma", "Armas", "Tenencia", "arma, tenencia, ilegal"),
            new("DISPAROS", "Disparos", "Armas", "Uso", "disparo, tiros, balas"),

            new("NARCOMENUDEO", "Narcomenudeo", "Drogas", "Venta", "drogas, venta, narco"),
            new("TENENCIA_DROGAS", "Tenencia de drogas", "Drogas", "Tenencia", "drogas, tenencia"),
            new("VENTA_DROGAS", "Venta de drogas", "Drogas", "Venta", "drogas, venta, dealer"),
            new("LABORATORIO", "Laboratorio clandestino", "Drogas", "Produccion", "laboratorio, produccion, drogas"),

            new("ACCIDENTE_TRANSITO", "Accidente de transito", "Seguridad vial", "Accidente", "choque, colision, transito"),
            new("ATROPELLO", "Atropello", "Seguridad vial", "Accidente", "atropello, peaton"),
            new("VUELCO", "Vuelco", "Seguridad vial", "Accidente", "vuelco, auto, camion"),
            new("INCENDIO_VEHICULO", "Incendio de vehiculo", "Seguridad vial", "Incendio", "incendio, vehiculo, auto"),

            new("INCENDIO", "Incendio", "Emergencias", "Incendio", "fuego, incendio, humo"),
            new("INCENDIO_FORESTAL", "Incendio forestal", "Emergencias", "Incendio", "fuego, forestal, monte"),
            new("FUGA_GAS", "Fuga de gas", "Emergencias", "Riesgo", "gas, fuga, riesgo"),
            new("SOSPECHA_BOMBA", "Sospecha de bomba", "Emergencias", "Riesgo", "bomba, explosivo, sospecha"),
            new("PERSONA_EXTRAVIADA", "Persona extraviada", "Emergencias", "Busqueda", "extraviada, perdida, busqueda"),
            new("DISTURBIOS", "Disturbios", "Orden publico", "Disturbios", "disturbios, desorden, tumulto"),
            new("RUIDOS_MOLESTOS", "Ruidos molestos", "Orden publico", "Convivencia", "ruidos, musica, molestos")
        };

        public static void SeedIfEmpty()
        {
            try
            {
                using var conn = Db.Open();
                using var countCmd = new SqlCommand("SELECT COUNT(1) FROM [sys_mapeo].[CatalogoHechos]", conn);
                var count = Convert.ToInt32(countCmd.ExecuteScalar());
                if (count > 0)
                    return;

                foreach (var item in Items)
                {
                    const string sql = @"
INSERT INTO [sys_mapeo].[CatalogoHechos]
([Codigo], [Nombre], [Categoria], [Subcategoria], [PalabrasClave], [Activo], [CreatedAt])
VALUES (@Codigo, @Nombre, @Categoria, @Subcategoria, @PalabrasClave, @Activo, @CreatedAt);";

                    using var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@Codigo", item.Codigo);
                    cmd.Parameters.AddWithValue("@Nombre", item.Nombre);
                    cmd.Parameters.AddWithValue("@Categoria", item.Categoria);
                    cmd.Parameters.AddWithValue("@Subcategoria", item.Subcategoria);
                    cmd.Parameters.AddWithValue("@PalabrasClave", item.PalabrasClave);
                    cmd.Parameters.AddWithValue("@Activo", 1);
                    cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CatalogoHechosSeeder] Error: {ex.Message}");
            }
        }
    }
}
