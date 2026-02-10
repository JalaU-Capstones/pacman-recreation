#!/usr/bin/env python3
"""
Generador de Sprite Sheet de Tiles del Laberinto - Estilo 8-bit Clásico
Genera todos los tiles necesarios para construir el mapa de Pac-Man
"""

from PIL import Image, ImageDraw

# Configuración
SPRITE_SIZE = 32  # Tamaño de cada sprite individual
WALL_THICKNESS = 4  # Grosor de las paredes

# Colores clásicos de Pac-Man (RGB)
COLORS = {
    'wall': (33, 33, 255),        # Azul clásico de las paredes
    'wall_fill': (0, 0, 0),       # Negro para el interior
    'background': (0, 0, 0),       # Negro para el fondo
    'door': (255, 184, 255),      # Rosa para la puerta
    'door_dark': (200, 100, 200)  # Rosa oscuro
}

BACKGROUND = (0, 0, 0, 0)  # Transparente para sprites
TRANSPARENT = (0, 0, 0, 0)

def create_wall_horizontal():
    """
    Crea un tile de pared horizontal
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    # Pared horizontal en el centro
    wall_y = size // 2 - WALL_THICKNESS // 2

    # Pared completa (sólida)
    draw.rectangle([0, wall_y, size, wall_y + WALL_THICKNESS], fill=COLORS['wall'])

    return img

def create_wall_vertical():
    """
    Crea un tile de pared vertical
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    # Pared vertical en el centro
    wall_x = size // 2 - WALL_THICKNESS // 2

    # Pared completa (sólida)
    draw.rectangle([wall_x, 0, wall_x + WALL_THICKNESS, size], fill=COLORS['wall'])

    return img

def create_wall_corner_tl():
    """
    Crea esquina superior izquierda (Top-Left)
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    center = size // 2

    # Pared horizontal (izquierda) - sólida
    wall_y = center - WALL_THICKNESS // 2
    draw.rectangle([0, wall_y, center, wall_y + WALL_THICKNESS], fill=COLORS['wall'])

    # Pared vertical (arriba) - sólida
    wall_x = center - WALL_THICKNESS // 2
    draw.rectangle([wall_x, 0, wall_x + WALL_THICKNESS, center], fill=COLORS['wall'])

    # Esquina redondeada
    corner_radius = WALL_THICKNESS
    corner_bbox = [center - corner_radius, center - corner_radius,
                   center + corner_radius, center + corner_radius]

    # Dibujar arco exterior
    draw.arc(corner_bbox, start=0, end=90, fill=COLORS['wall'], width=2)

    return img

def create_wall_corner_tr():
    """
    Crea esquina superior derecha (Top-Right)
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    center = size // 2

    # Pared horizontal (derecha) - sólida
    wall_y = center - WALL_THICKNESS // 2
    draw.rectangle([center, wall_y, size, wall_y + WALL_THICKNESS], fill=COLORS['wall'])

    # Pared vertical (arriba) - sólida
    wall_x = center - WALL_THICKNESS // 2
    draw.rectangle([wall_x, 0, wall_x + WALL_THICKNESS, center], fill=COLORS['wall'])

    # Esquina redondeada
    corner_radius = WALL_THICKNESS
    corner_bbox = [center - corner_radius, center - corner_radius,
                   center + corner_radius, center + corner_radius]
    draw.arc(corner_bbox, start=90, end=180, fill=COLORS['wall'], width=2)

    return img

def create_wall_corner_bl():
    """
    Crea esquina inferior izquierda (Bottom-Left)
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    center = size // 2

    # Pared horizontal (izquierda) - sólida
    wall_y = center - WALL_THICKNESS // 2
    draw.rectangle([0, wall_y, center, wall_y + WALL_THICKNESS], fill=COLORS['wall'])

    # Pared vertical (abajo) - sólida
    wall_x = center - WALL_THICKNESS // 2
    draw.rectangle([wall_x, center, wall_x + WALL_THICKNESS, size], fill=COLORS['wall'])

    # Esquina redondeada
    corner_radius = WALL_THICKNESS
    corner_bbox = [center - corner_radius, center - corner_radius,
                   center + corner_radius, center + corner_radius]
    draw.arc(corner_bbox, start=270, end=360, fill=COLORS['wall'], width=2)

    return img

def create_wall_corner_br():
    """
    Crea esquina inferior derecha (Bottom-Right)
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    center = size // 2

    # Pared horizontal (derecha) - sólida
    wall_y = center - WALL_THICKNESS // 2
    draw.rectangle([center, wall_y, size, wall_y + WALL_THICKNESS], fill=COLORS['wall'])

    # Pared vertical (abajo) - sólida
    wall_x = center - WALL_THICKNESS // 2
    draw.rectangle([wall_x, center, wall_x + WALL_THICKNESS, size], fill=COLORS['wall'])

    # Esquina redondeada
    corner_radius = WALL_THICKNESS
    corner_bbox = [center - corner_radius, center - corner_radius,
                   center + corner_radius, center + corner_radius]
    draw.arc(corner_bbox, start=180, end=270, fill=COLORS['wall'], width=2)

    return img

def create_wall_t_junction(direction):
    """
    Crea una unión en T

    Args:
        direction: 'up', 'down', 'left', 'right' (indica hacia dónde apunta la T)
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    center = size // 2
    wall_x = center - WALL_THICKNESS // 2
    wall_y = center - WALL_THICKNESS // 2

    if direction == 'up':
        # T pointing UP - has horizontal wall on bottom + vertical wall on top
        # Horizontal completo - sólido
        draw.rectangle([0, wall_y, size, wall_y + WALL_THICKNESS], fill=COLORS['wall'])

        # Vertical hacia arriba - sólido
        draw.rectangle([wall_x, 0, wall_x + WALL_THICKNESS, center], fill=COLORS['wall'])

    elif direction == 'down':
        # T pointing DOWN - has horizontal wall on top + vertical wall on bottom
        # Horizontal completo - sólido
        draw.rectangle([0, wall_y, size, wall_y + WALL_THICKNESS], fill=COLORS['wall'])

        # Vertical hacia abajo - sólido
        draw.rectangle([wall_x, center, wall_x + WALL_THICKNESS, size], fill=COLORS['wall'])

    elif direction == 'left':
        # T pointing LEFT - has vertical wall on right + horizontal wall on left
        # Vertical completo - sólido
        draw.rectangle([wall_x, 0, wall_x + WALL_THICKNESS, size], fill=COLORS['wall'])

        # Horizontal hacia izquierda - sólido
        draw.rectangle([0, wall_y, center, wall_y + WALL_THICKNESS], fill=COLORS['wall'])

    elif direction == 'right':
        # T pointing RIGHT - has vertical wall on left + horizontal wall on right
        # Vertical completo - sólido
        draw.rectangle([wall_x, 0, wall_x + WALL_THICKNESS, size], fill=COLORS['wall'])

        # Horizontal hacia derecha - sólido
        draw.rectangle([center, wall_y, size, wall_y + WALL_THICKNESS], fill=COLORS['wall'])

    return img

def create_wall_cross():
    """
    Crea una cruz (intersección de 4 caminos)
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    center = size // 2
    wall_x = center - WALL_THICKNESS // 2
    wall_y = center - WALL_THICKNESS // 2

    # Horizontal completo - sólido
    draw.rectangle([0, wall_y, size, wall_y + WALL_THICKNESS], fill=COLORS['wall'])

    # Vertical completo - sólido
    draw.rectangle([wall_x, 0, wall_x + WALL_THICKNESS, size], fill=COLORS['wall'])

    return img

def create_wall_end(direction):
    """
    Crea un terminal de pared (pared que termina)

    Args:
        direction: 'up', 'down', 'left', 'right'
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    center = size // 2
    wall_x = center - WALL_THICKNESS // 2
    wall_y = center - WALL_THICKNESS // 2

    if direction == 'up':
        # Vertical hacia arriba - sólida
        draw.rectangle([wall_x, 0, wall_x + WALL_THICKNESS, center + WALL_THICKNESS // 2],
                       fill=COLORS['wall'])

        # Tapa superior
        draw.rectangle([wall_x, 0, wall_x + WALL_THICKNESS, 2], fill=COLORS['wall'])

    elif direction == 'down':
        # Vertical hacia abajo - sólida
        draw.rectangle([wall_x, center - WALL_THICKNESS // 2, wall_x + WALL_THICKNESS, size],
                       fill=COLORS['wall'])

        # Tapa inferior
        draw.rectangle([wall_x, size - 2, wall_x + WALL_THICKNESS, size],
                       fill=COLORS['wall'])

    elif direction == 'left':
        # Horizontal hacia izquierda - sólida
        draw.rectangle([0, wall_y, center + WALL_THICKNESS // 2, wall_y + WALL_THICKNESS],
                       fill=COLORS['wall'])

        # Tapa izquierda
        draw.rectangle([0, wall_y, 2, wall_y + WALL_THICKNESS], fill=COLORS['wall'])

    elif direction == 'right':
        # Horizontal hacia derecha - sólida
        draw.rectangle([center - WALL_THICKNESS // 2, wall_y, size, wall_y + WALL_THICKNESS],
                       fill=COLORS['wall'])

        # Tapa derecha
        draw.rectangle([size - 2, wall_y, size, wall_y + WALL_THICKNESS],
                       fill=COLORS['wall'])

    return img

def create_ghost_door():
    """
    Crea la puerta de la casa de los fantasmas
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    center = size // 2
    door_height = 3

    # Línea horizontal de la puerta
    door_y = center - door_height // 2

    # Dibujar línea con patrón discontinuo
    segment_length = 4
    gap_length = 2

    x = 0
    while x < size:
        draw.rectangle([x, door_y, min(x + segment_length, size), door_y + door_height],
                      fill=COLORS['door'])
        x += segment_length + gap_length

    return img

def create_empty_tile():
    """
    Crea un tile vacío (fondo negro)
    """
    size = SPRITE_SIZE
    img = Image.new('RGBA', (size, size), TRANSPARENT)
    draw = ImageDraw.Draw(img)

    # Fondo completamente negro
    draw.rectangle([0, 0, size, size], fill=COLORS['background'])

    return img

def create_tiles_spritesheet():
    """
    Crea el sprite sheet completo de tiles del laberinto

    Layout (2 filas):
    Fila 1: [H][V][TL][TR][BL][BR][T-Up][T-Down]
    Fila 2: [T-Left][T-Right][Cross][End-Up][End-Down][End-Left][End-Right][Door][Empty]
    """

    # Dimensiones del sprite sheet
    cols = 9
    rows = 2

    sheet_width = SPRITE_SIZE * cols
    sheet_height = SPRITE_SIZE * rows

    sprite_sheet = Image.new('RGBA', (sheet_width, sheet_height), TRANSPARENT)

    # Primera fila
    row1_sprites = [
        create_wall_horizontal(),      # 0: Horizontal
        create_wall_vertical(),        # 1: Vertical
        create_wall_corner_tl(),       # 2: Corner Top-Left
        create_wall_corner_tr(),       # 3: Corner Top-Right
        create_wall_corner_bl(),       # 4: Corner Bottom-Left
        create_wall_corner_br(),       # 5: Corner Bottom-Right
        create_wall_t_junction('up'),  # 6: T-Junction Up
        create_wall_t_junction('down'),# 7: T-Junction Down
        create_wall_t_junction('left') # 8: T-Junction Left
    ]

    for col, sprite in enumerate(row1_sprites):
        x = col * SPRITE_SIZE
        y = 0
        sprite_sheet.paste(sprite, (x, y))

    # Segunda fila
    row2_sprites = [
        create_wall_t_junction('right'), # 0: T-Junction Right
        create_wall_cross(),             # 1: Cross
        create_wall_end('up'),           # 2: End Up
        create_wall_end('down'),         # 3: End Down
        create_wall_end('left'),         # 4: End Left
        create_wall_end('right'),        # 5: End Right
        create_ghost_door(),             # 6: Ghost Door
        create_empty_tile(),             # 7: Empty/Background
        create_empty_tile()              # 8: Extra empty
    ]

    for col, sprite in enumerate(row2_sprites):
        x = col * SPRITE_SIZE
        y = SPRITE_SIZE
        sprite_sheet.paste(sprite, (x, y))

    return sprite_sheet

def create_sprite_map_json():
    """
    Crea un archivo JSON con las coordenadas de cada sprite
    """
    sprite_map = {
        "sprite_size": SPRITE_SIZE,
        "sprites": {
            "walls": {
                "horizontal": {"x": 0, "y": 0},
                "vertical": {"x": 32, "y": 0},
                "corner_tl": {"x": 64, "y": 0},
                "corner_tr": {"x": 96, "y": 0},
                "corner_bl": {"x": 128, "y": 0},
                "corner_br": {"x": 160, "y": 0},
                "t_up": {"x": 192, "y": 0},
                "t_down": {"x": 224, "y": 0},
                "t_left": {"x": 256, "y": 0},
                "t_right": {"x": 0, "y": 32},
                "cross": {"x": 32, "y": 32},
                "end_up": {"x": 64, "y": 32},
                "end_down": {"x": 96, "y": 32},
                "end_left": {"x": 128, "y": 32},
                "end_right": {"x": 160, "y": 32}
            },
            "special": {
                "ghost_door": {"x": 192, "y": 32},
                "empty": {"x": 224, "y": 32}
            }
        }
    }

    import json
    with open('tiles_sprite_map.json', 'w') as f:
        json.dump(sprite_map, f, indent=2)

    print("✅ Archivo JSON de mapeo creado: tiles_sprite_map.json")

def main():
    print("🧱 Generador de Sprites de Tiles del Laberinto")
    print("=" * 50)

    # Generar sprite sheet de tiles
    print("Generando sprite sheet de tiles...")
    tiles_sheet = create_tiles_spritesheet()

    # Guardar sprite sheet
    output_path = 'tiles_spritesheet.png'
    tiles_sheet.save(output_path)
    print(f"✅ Sprite sheet guardado: {output_path}")

    # Crear mapa de sprites (JSON)
    create_sprite_map_json()

    # Información del sprite sheet
    print("\n📊 Información del Sprite Sheet:")
    print(f"   - Tamaño total: {tiles_sheet.width}x{tiles_sheet.height} píxeles")
    print(f"   - Tamaño de sprite individual: {SPRITE_SIZE}x{SPRITE_SIZE} píxeles")
    print(f"   - Total de tiles: 17")
    print(f"   - Distribución:")
    print(f"     • Pared horizontal: 1 tile")
    print(f"     • Pared vertical: 1 tile")
    print(f"     • Esquinas: 4 tiles (TL, TR, BL, BR)")
    print(f"     • Uniones en T: 4 tiles (arriba, abajo, izq, der)")
    print(f"     • Cruz (intersección): 1 tile")
    print(f"     • Terminales: 4 tiles (arriba, abajo, izq, der)")
    print(f"     • Puerta de fantasmas: 1 tile")
    print(f"     • Fondo vacío: 1 tile")

    print("\n💡 Uso de los tiles:")
    print("   - Color azul clásico (#2121FF) para las paredes")
    print("   - Grosor de pared: 4 píxeles")
    print("   - Diseño modular para construir cualquier laberinto")
    print("   - Puerta rosa para la casa de los fantasmas")

    print("\n✨ ¡Generación completada!")
    print(f"   Archivos creados:")
    print(f"   - tiles_spritesheet.png")
    print(f"   - tiles_sprite_map.json")

if __name__ == "__main__":
    main()
