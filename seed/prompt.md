Transforma las recetas crudas de Spoonacular al formato JSON de seed para Nido.

Responde solo JSON valido. No agregues explicaciones, markdown, comentarios ni texto fuera del JSON.

Objetivo:
- Traducir y normalizar recetas para insertarlas luego en la base de datos de Nido.
- El JSON debe coincidir con estas tablas:
  - productos
  - recetas
  - ingredientes_receta
  - pasos_receta
  - info_nutricional_receta
  - receta_electrodomestico

Reglas generales:
- Todo texto visible para usuarios debe estar en espanol rioplatense neutro.
- No uses HTML. Si el origen trae HTML en summary o instructions, convertilo a texto plano.
- No inventes datos precisos que no existan. Si falta un dato, usa null.
- Usa numeros para cantidades, porciones, tiempo y nutricion. No uses strings numericos.
- Mantene unidades cortas y simples: g, kg, ml, l, unidad, cda, cdta, taza, pizca.
- Si una unidad de Spoonacular no tiene buen equivalente, traducila a una unidad clara.
- No incluyas IDs UUID. La migracion o script de seed los generara.
- Evita duplicar productos: si varias recetas usan "tomate", debe aparecer una sola vez en productos.
- El campo fuenteId debe ser unico y conservar el id de Spoonacular con este formato: "spoonacular-{id}".

Formato exacto de respuesta:
{
  "productos": [
    {
      "nombre": "string",
      "codigoBarras": null,
      "imagenUrl": "string|null",
      "categoriaNombre": "string|null"
    }
  ],
  "recetas": [
    {
      "nombre": "string",
      "descripcion": "string|null",
      "tiempoCoccionMin": 0,
      "dificultad": "Facil|Media|Dificil",
      "porciones": 0,
      "fuenteId": "spoonacular-123",
      "imagenUrl": "string|null",
      "ingredientes": [
        {
          "productoNombre": "string",
          "nombreIngrediente": "string",
          "cantidad": 0,
          "unidad": "string|null"
        }
      ],
      "pasos": [
        {
          "orden": 1,
          "descripcion": "string"
        }
      ],
      "nutricion": {
        "calorias": 0,
        "proteinas": 0,
        "carbohidratos": 0,
        "grasas": 0
      },
      "electrodomesticos": [
        "string"
      ]
    }
  ]
}

Mapeo a base de datos:
- productos.nombre -> productos.nombre
- productos.codigoBarras -> productos.codigo_barras
- productos.imagenUrl -> productos.imagen_url
- productos.categoriaNombre se usara para buscar o crear categorias_producto.nombre. Si no hay categoria clara, usa null.

- recetas.nombre -> recetas.nombre
- recetas.descripcion -> recetas.descripcion
- recetas.tiempoCoccionMin -> recetas.tiempo_coccion_min
- recetas.dificultad -> recetas.dificultad
- recetas.porciones -> recetas.porciones
- recetas.fuenteId -> recetas.fuente_id
- recetas.imagenUrl -> recetas.imagen_url

- ingredientes.productoNombre debe coincidir exactamente con algun productos.nombre.
- ingredientes.nombreIngrediente -> ingredientes_receta.nombre_ingrediente
- ingredientes.cantidad -> ingredientes_receta.cantidad
- ingredientes.unidad -> ingredientes_receta.unidad

- pasos.orden -> pasos_receta.orden
- pasos.descripcion -> pasos_receta.descripcion

- nutricion.calorias -> info_nutricional_receta.calorias
- nutricion.proteinas -> info_nutricional_receta.proteinas
- nutricion.carbohidratos -> info_nutricional_receta.carbohidratos
- nutricion.grasas -> info_nutricional_receta.grasas

- electrodomesticos[] -> receta_electrodomestico.tipo_requerido

Valores permitidos:
- dificultad solo puede ser: "Facil", "Media", "Dificil".
- electrodomesticos solo puede contener estos valores:
  - "Licuadora"
  - "Microondas"
  - "Horno/Cocina"
  - "Mixer"
  - "Procesadora"
  - "Freidora de aire"
  - "Cafetera"
  - "Tostadora"
  - "Olla de presion"
  - "Parrilla electrica"

Reglas de dificultad:
- "Facil": receta simple, pocos pasos o hasta 30 minutos.
- "Media": receta con varios pasos o entre 31 y 60 minutos.
- "Dificil": receta larga, tecnica o de mas de 60 minutos.

Reglas de productos e ingredientes:
- productoNombre debe ser un nombre canonico y reutilizable para alacena, por ejemplo "Tomate", "Cebolla", "Fideos", "Aceite de oliva".
- nombreIngrediente puede ser mas especifico, por ejemplo "Tomate perita picado".
- Si Spoonacular trae "1 large onion", normaliza como:
  - productoNombre: "Cebolla"
  - nombreIngrediente: "Cebolla grande"
  - cantidad: 1
  - unidad: "unidad"
- Si no se puede determinar cantidad, usa cantidad: null.
- Si no se puede determinar unidad, usa unidad: null.

Reglas de pasos:
- Usa analyzedInstructions si existe.
- Si no hay analyzedInstructions pero hay instructions, separa el texto en pasos claros.
- Si no hay instrucciones, usa pasos: [].
- Los pasos deben empezar en orden 1 y aumentar de a 1.

Reglas de nutricion:
- Extrae de nutrition.nutrients si existe.
- Usa:
  - "Calories" para calorias
  - "Protein" para proteinas
  - "Carbohydrates" para carbohidratos
  - "Fat" para grasas
- Si falta alguno, usa null.

Reglas de electrodomesticos:
- Si la receta requiere horno, sarten, olla, cacerola, hornalla o coccion general, usa "Horno/Cocina".
- Si requiere blender/licuadora, usa "Licuadora".
- Si requiere microwave, usa "Microondas".
- Si requiere air fryer, usa "Freidora de aire".
- Si requiere grill/parrilla, usa "Parrilla electrica".
- Si requiere food processor, usa "Procesadora".
- Si no se puede inferir, usa ["Horno/Cocina"].

Recetas fuente de Spoonacular:

