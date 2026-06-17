-- =============================================
-- BASE DE DATOS: NIDO
-- =============================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =============================================
-- TABLAS SIN DEPENDENCIAS (se crean primero)
-- =============================================

CREATE TABLE usuarios (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    nombre          VARCHAR(255) NOT NULL,
    email           VARCHAR(255) NOT NULL UNIQUE,
    password_hash   VARCHAR(255),
    oauth_provider  VARCHAR(100),
    oauth_id        VARCHAR(255),
    created_at      TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE hogares (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    nombre      VARCHAR(255) NOT NULL,
    created_at  TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE categorias_producto (
    id      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    nombre  VARCHAR(255) NOT NULL,
    ttl_dias  INT
);

CREATE TABLE logros (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    nombre      VARCHAR(255) NOT NULL,
    descripcion TEXT,
    icono_url   VARCHAR(500)
);

CREATE TABLE recetas (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    nombre              VARCHAR(255) NOT NULL,
    descripcion         TEXT,
    tiempo_coccion_min  INT,
    dificultad          VARCHAR(100),
    porciones           INT,
    fuente_id           VARCHAR(255)
);

CREATE TABLE electrodomesticos (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    hogar_id    UUID NOT NULL,
    nombre      VARCHAR(255) NOT NULL,
    tipo        VARCHAR(100),
    estado      VARCHAR(100),
    FOREIGN KEY (hogar_id) REFERENCES hogares(id)
);

-- =============================================
-- TABLAS CON DEPENDENCIAS DE PRIMER NIVEL
-- =============================================

CREATE TABLE miembros_hogar (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    usuario_id  UUID NOT NULL,
    hogar_id    UUID NOT NULL,
    rol         VARCHAR(100),
    puntos      INT DEFAULT 0,
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id),
    FOREIGN KEY (hogar_id)   REFERENCES hogares(id)
);

CREATE TABLE restricciones_usuario (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    usuario_id  UUID NOT NULL,
    tipo        VARCHAR(100),
    descripcion VARCHAR(500),
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id)
);

CREATE TABLE notificaciones (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    usuario_id      UUID NOT NULL,
    tipo            VARCHAR(100),
    mensaje         TEXT,
    leida           BOOLEAN DEFAULT FALSE,
    referencia_id   UUID,
    referencia_tipo VARCHAR(100),
    created_at      TIMESTAMP NOT NULL DEFAULT NOW(),
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id)
);

CREATE TABLE invitaciones_hogar (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    hogar_id        UUID NOT NULL,
    invitado_por    UUID NOT NULL,
    codigo          VARCHAR(255),
    token           VARCHAR(255),
    estado          VARCHAR(100),
    expira_en       TIMESTAMP,
    created_at      TIMESTAMP NOT NULL DEFAULT NOW(),
    FOREIGN KEY (hogar_id)      REFERENCES hogares(id),
    FOREIGN KEY (invitado_por)  REFERENCES usuarios(id)
);

CREATE TABLE productos (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    nombre          VARCHAR(255) NOT NULL,
    codigo_barras   VARCHAR(255),
    imagen_url      VARCHAR(500),
    categoria_id    UUID,
    FOREIGN KEY (categoria_id) REFERENCES categorias_producto(id)
);

CREATE TABLE logros_usuario (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    usuario_id      UUID NOT NULL,
    logro_id        UUID NOT NULL,
    fecha_obtenido  TIMESTAMP NOT NULL DEFAULT NOW(),
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id),
    FOREIGN KEY (logro_id)   REFERENCES logros(id)
);

CREATE TABLE logros_hogar (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    hogar_id        UUID NOT NULL,
    logro_id        UUID NOT NULL,
    FOREIGN KEY (hogar_id) REFERENCES hogares(id),
    FOREIGN KEY (logro_id) REFERENCES logros(id)
);

CREATE TABLE recetas_cocinadas (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    hogar_id            UUID NOT NULL,
    receta_id           UUID NOT NULL,
    cocinado_por        UUID NOT NULL,
    porciones_cocinadas INT,
    fecha               TIMESTAMP NOT NULL DEFAULT NOW(),
    FOREIGN KEY (hogar_id)      REFERENCES hogares(id),
    FOREIGN KEY (receta_id)     REFERENCES recetas(id),
    FOREIGN KEY (cocinado_por)  REFERENCES usuarios(id)
);

CREATE TABLE receta_electrodomestico (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    receta_id       UUID NOT NULL,
    tipo_requerido  VARCHAR(100),
    FOREIGN KEY (receta_id) REFERENCES recetas(id)
);

CREATE TABLE info_nutricional_receta (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    receta_id       UUID NOT NULL,
    calorias        DECIMAL(10,2),
    proteinas       DECIMAL(10,2),
    carbohidratos   DECIMAL(10,2),
    grasas          DECIMAL(10,2),
    FOREIGN KEY (receta_id) REFERENCES recetas(id)
);

CREATE TABLE info_nutricional_producto (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    producto_id     UUID NOT NULL,
    calorias        DECIMAL(10,2),
    proteinas       DECIMAL(10,2),
    carbohidratos   DECIMAL(10,2),
    grasas          DECIMAL(10,2),
    FOREIGN KEY (producto_id) REFERENCES productos(id)
);

CREATE TABLE ingredientes_receta (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    receta_id           UUID NOT NULL,
    nombre_ingrediente   VARCHAR(255) NOT NULL,
    producto_id         UUID,
    cantidad            DECIMAL(10,2),
    unidad              VARCHAR(100),
    FOREIGN KEY (receta_id)     REFERENCES recetas(id),
    FOREIGN KEY (producto_id)   REFERENCES productos(id)
);

-- =============================================
-- TABLAS CON DEPENDENCIAS DE SEGUNDO NIVEL
-- =============================================

CREATE TABLE stock_hogar (
    id                    UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    hogar_id              UUID NOT NULL,
    producto_id           UUID NOT NULL,
    cargado_por           UUID NOT NULL,
    cantidad_actual       DECIMAL(10,2),
    unidad_medida         VARCHAR(100),
    fecha_vencimiento     DATE,
    ubicacion             VARCHAR(100) NOT NULL DEFAULT 'Alacena',
    esta_abierto          BOOLEAN NOT NULL DEFAULT FALSE,
    porcentaje_consumido  DECIMAL(5,2) NOT NULL DEFAULT 0,
    created_at            TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_by            UUID NOT NULL,
    updated_at            TIMESTAMP,
    FOREIGN KEY (hogar_id)      REFERENCES hogares(id),
    FOREIGN KEY (producto_id)   REFERENCES productos(id),
    FOREIGN KEY (cargado_por)   REFERENCES usuarios(id),
    FOREIGN KEY (updated_by)    REFERENCES usuarios(id)
);

CREATE TABLE lista_compras (
    id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    hogar_id                UUID NOT NULL,
    producto_id             UUID NOT NULL,
    agregado_por            UUID NOT NULL,
    cantidad                DECIMAL(10,2),
    unidad                  VARCHAR(100),
    comprado                BOOLEAN DEFAULT FALSE,
    agregado_al_inventario  BOOLEAN DEFAULT FALSE,
    grupo_nombre            VARCHAR(255) NOT NULL DEFAULT 'Productos agregados',
    orden                   INTEGER NOT NULL DEFAULT 0,
    created_at              TIMESTAMP DEFAULT now(),
    comprado_en             TIMESTAMP,
    comprado_por            UUID,
    removido_de_lista_at    TIMESTAMP,
    producto_nombre_snapshot VARCHAR(255) NOT NULL DEFAULT '',
    FOREIGN KEY (hogar_id)      REFERENCES hogares(id),
    FOREIGN KEY (producto_id)   REFERENCES productos(id),
    FOREIGN KEY (agregado_por)  REFERENCES usuarios(id),
    FOREIGN KEY (comprado_por)  REFERENCES usuarios(id)
);

CREATE TABLE tareas (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    hogar_id            UUID NOT NULL,
    creado_por          UUID NOT NULL,
    titulo              VARCHAR(255) NOT NULL,
    descripcion         TEXT,
    estado              VARCHAR(100),
    fecha_limite        TIMESTAMP,
    completado_por      UUID NOT NULL,
    fecha_completado    TIMESTAMP,
    created_at          TIMESTAMP NOT NULL DEFAULT NOW(),
    FOREIGN KEY (hogar_id)      REFERENCES hogares(id),
    FOREIGN KEY (creado_por)    REFERENCES usuarios(id),
    FOREIGN KEY (completado_por) REFERENCES usuarios(id)
);

CREATE TABLE gastos (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    hogar_id    UUID NOT NULL,
    pagado_por  UUID NOT NULL,
    monto       DECIMAL(10,2) NOT NULL,
    descripcion VARCHAR(500),
    categoria   VARCHAR(100),
    fecha       DATE NOT NULL,
    created_at  TIMESTAMP NOT NULL DEFAULT NOW(),
    FOREIGN KEY (hogar_id)   REFERENCES hogares(id),
    FOREIGN KEY (pagado_por) REFERENCES usuarios(id)
);

CREATE TABLE asignaciones_tarea (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tarea_id        UUID NOT NULL,
    usuario_id      UUID NOT NULL,
    fecha_asignacion TIMESTAMP NOT NULL DEFAULT NOW(),
    FOREIGN KEY (tarea_id)   REFERENCES tareas(id),
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id)
);


CREATE TABLE resenias_receta (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    receta_id   UUID NOT NULL,
    usuario_id  UUID NOT NULL,
    puntuacion INT CHECK (puntuacion >= 1 AND puntuacion <= 5),
    comentario   TEXT,
    created_at  TIMESTAMP NOT NULL DEFAULT NOW(),
    FOREIGN KEY (receta_id)   REFERENCES recetas(id),
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id)
);

-- =============================================
-- ÍNDICES útiles para búsquedas frecuentes
-- =============================================

CREATE INDEX idx_miembros_hogar_hogar     ON miembros_hogar(hogar_id);
CREATE INDEX idx_miembros_hogar_usuario   ON miembros_hogar(usuario_id);
CREATE INDEX idx_stock_hogar_hogar        ON stock_hogar(hogar_id);
CREATE INDEX idx_lista_compras_hogar      ON lista_compras(hogar_id);
CREATE INDEX idx_lista_compras_comprado_por ON lista_compras(comprado_por);
CREATE INDEX idx_tareas_hogar             ON tareas(hogar_id);
CREATE INDEX idx_gastos_hogar             ON gastos(hogar_id);
CREATE INDEX idx_notificaciones_usuario   ON notificaciones(usuario_id);
CREATE INDEX idx_ingredientes_receta      ON ingredientes_receta(receta_id);

-- =============================================
-- SEED DE DESARROLLO
-- Placeholder hasta que haya login implementado.
-- El front usa hogarId/usuarioId = 000...001
-- =============================================

INSERT INTO hogares (id, nombre)
VALUES ('00000000-0000-0000-0000-000000000001', 'Hogar de Prueba')
ON CONFLICT (id) DO NOTHING;

INSERT INTO usuarios (id, nombre, email)
VALUES ('00000000-0000-0000-0000-000000000001', 'Usuario Dev', 'dev@nido.test')
ON CONFLICT (id) DO NOTHING;

INSERT INTO miembros_hogar (id, hogar_id, usuario_id, rol)
VALUES ('00000000-0000-0000-0000-000000000001',
        '00000000-0000-0000-0000-000000000001',
        '00000000-0000-0000-0000-000000000001',
        'admin')
ON CONFLICT (id) DO NOTHING;
