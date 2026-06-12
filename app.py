from flask import Flask, request, jsonify
import pg8000

app = Flask(__name__)

# =====================================================================
# 1. FUNCIÓN QUE CONECTA A TU POSTGRES EN DOCKER
# =====================================================================
def obtener_recetas_de_postgres():
    conn = pg8000.connect(
        user="root",
        password="root", 
        host="localhost",
        port=5432,
        database="nido"
    )
    cursor = conn.cursor()
    cursor.execute("SELECT nombre FROM recetas LIMIT 10;")
    filas = cursor.fetchall()
    cursor.close()
    conn.close()
    return [fila[0] for fila in filas]

# =====================================================================
# 2. MOCK INTELIGENTE (SIMULADOR DE IA)
# =====================================================================
def procesar_pedido_con_mock(texto_usuario, recetas_disponibles):
    texto = texto_usuario.lower().strip()
    
    if "sopa" in texto or "lentejas" in texto:
        return "Sopa de lentejas rojas con pollo y nabo"
    elif "zucchini" in texto or "calabacin" in texto:
        return "Arroz con zucchini"
    elif "fideos" in texto or "pasta" in texto:
        if "atun" in texto or "pescado" in texto:
            return "Pasta con atun"
        return "Pasta ratatouille"
    elif "pasteles" in texto or "carne" in texto:
        return "Pasteles de carne de Natchitoches"
    elif "almendras" in texto or "arvejas" in texto:
        return "Arroz con almendras y arvejas"
    elif "pakistani" in texto:
        return "Arroz pakistaní"
    elif "mango" in texto:
        return "Arroz frito con mango"
    elif "integral" in texto:
        return "Arroz integral frito"
        
    return "NONE"

# =====================================================================
# 3. ENDPOINT PARA .NET
# =====================================================================
@app.route('/api/ia/recomendar', methods=['POST'])
def recomendar_receta():
    data = request.get_json() or {}
    mensaje_usuario = data.get('mensaje', '')
    
    if not mensaje_usuario:
        return jsonify({'error': 'Falta el campo "mensaje" en el JSON'}), 400
        
    print(f"📩 Pedido desde .NET: '{mensaje_usuario}'")
    
    try:
        recetas_en_bd = obtener_recetas_de_postgres()
        receta_sugerida = procesar_pedido_con_mock(mensaje_usuario, recetas_en_bd)
        
        print(f"🤖 Sugerencia enviada: '{receta_sugerida}'")
        return jsonify({'receta': receta_sugerida}), 200
        
    except Exception as e:
        print(f"❌ Error interno: {str(e)}")
        return jsonify({'error': 'Error interno del servidor de Python'}), 500

if __name__ == '__main__':
    print("🚀 Microservicio de IA escuchando en http://localhost:5000")
    app.run(host='0.0.0.0', port=5000, debug=True)