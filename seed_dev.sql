-- New columns: stock_hogar
ALTER TABLE stock_hogar ADD COLUMN IF NOT EXISTS ubicacion            VARCHAR(100) NOT NULL DEFAULT 'Alacena';
ALTER TABLE stock_hogar ADD COLUMN IF NOT EXISTS esta_abierto         BOOLEAN      NOT NULL DEFAULT FALSE;
ALTER TABLE stock_hogar ADD COLUMN IF NOT EXISTS porcentaje_consumido DECIMAL(5,2) NOT NULL DEFAULT 0;

-- New column: productos
ALTER TABLE productos ADD COLUMN IF NOT EXISTS imagen_url VARCHAR(500);

-- Dev seed (placeholder until auth is implemented)
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

SELECT 'OK' AS result;
