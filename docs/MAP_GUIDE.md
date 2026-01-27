# 🗺️ Guía de Mapas de Pac-Man

## 📋 Índice
1. [Formato del Mapa](#formato-del-mapa)
2. [Leyenda de Caracteres](#leyenda-de-caracteres)
3. [Especificaciones Técnicas](#especificaciones-técnicas)
4. [Mapas Incluidos](#mapas-incluidos)
5. [Cómo Crear Mapas Personalizados](#cómo-crear-mapas-personalizados)
6. [Validación de Mapas](#validación-de-mapas)

---

## 🎮 Formato del Mapa

Los mapas de Pac-Man están almacenados en archivos de texto plano (.txt) donde cada carácter representa un elemento del juego.

### Dimensiones Estándar:
- **Ancho:** 28 caracteres (columnas)
- **Alto:** 31 caracteres (filas)
- **Total de celdas:** 868 celdas

---

## 🔤 Leyenda de Caracteres

| Carácter | Elemento | Descripción | Comportamiento |
|----------|----------|-------------|----------------|
| `#` | **Pared** | Muro del laberinto | Bloquea el movimiento de Pac-Man y fantasmas |
| `.` | **Punto pequeño** | Píldora energizante | +10 puntos, Pac-Man puede atravesar |
| `o` | **Power Pellet** | Punto grande | +50 puntos, hace vulnerables a los fantasmas |
| `P` | **Pac-Man** | Posición inicial del jugador | Se reemplaza por espacio al iniciar |
| `G` | **Fantasma** | Posición inicial de fantasmas | Se reemplaza por espacio al iniciar |
| `-` | **Puerta** | Puerta de la casa de fantasmas | Solo fantasmas pueden atravesar |
| `F` | **Fruta** | Bonus opcional | +100 a +1000 puntos según tipo |
| ` ` | **Espacio vacío** | Celda sin contenido | Libre para movimiento |

---

## ⚙️ Especificaciones Técnicas

### Estructura del Mapa:

```
############################  ← Fila 1 (borde superior)
#............##............#  ← Fila 2 (pasillo con puntos)
#.####.#####.##.#####.####.#  ← Fila 3 (paredes internas)
...
#..........................#  ← Fila 30 (pasillo inferior)
############################  ← Fila 31 (borde inferior)
```

### Reglas de Diseño:

1. **Bordes Obligatorios:**
   - Primera fila: completamente `#`
   - Última fila: completamente `#`
   - Primera columna: siempre `#`
   - Última columna: siempre `#`

2. **Casa de Fantasmas:**
   - Debe estar en el centro del mapa
   - Rodeada por paredes `#`
   - Una puerta `-` para entrada/salida
   - 6 posiciones `G` para los fantasmas

3. **Posición de Pac-Man:**
   - Solo debe haber **UN** carácter `P` en todo el mapa
   - Generalmente en la parte inferior del laberinto
   - Rodeado de espacio libre para movimiento inicial

4. **Distribución de Puntos:**
   - **Puntos pequeños (`.`):** ~240-250 en el mapa
   - **Power Pellets (`o`):** Exactamente 4 (uno en cada esquina)

5. **Conectividad:**
   - Todos los pasillos deben estar conectados
   - No debe haber áreas aisladas
   - Debe ser posible llegar a todos los puntos

---

## 🎯 Mapas Incluidos

### Level 1 - Clásico (level1.txt)
```
Dificultad: ⭐⭐☆☆☆ (Fácil)
Características:
- Diseño clásico de Pac-Man original
- Pasillos amplios
- 4 power pellets en las esquinas
- Casa de fantasmas central
- Ideal para comenzar

Estadísticas:
- Puntos pequeños: ~244
- Power pellets: 4
- Total puntos posibles: ~2,640
```

### Level 2 - Intermedio (level2.txt)
```
Dificultad: ⭐⭐⭐☆☆ (Medio)
Características:
- Más paredes internas
- Pasillos más estrechos
- Mayor dificultad para escapar
- Power pellets en posiciones estratégicas

Estadísticas:
- Puntos pequeños: ~228
- Power pellets: 4
- Total puntos posibles: ~2,480
```

### Level 3 - Avanzado (level3.txt)
```
Dificultad: ⭐⭐⭐⭐☆ (Difícil)
Características:
- Laberinto complejo
- Callejones sin salida
- Menos espacio de maniobra
- Requiere estrategia avanzada

Estadísticas:
- Puntos pequeños: ~220
- Power pellets: 4
- Total puntos posibles: ~2,400
```

---

## 🛠️ Cómo Crear Mapas Personalizados

### Paso 1: Crear el archivo
```
Archivo: levelX.txt (donde X es el número del nivel)
Codificación: UTF-8 sin BOM
Fin de línea: LF o CRLF (ambos funcionan)
```

### Paso 2: Diseñar el contorno
```
############################
#                          #
#                          #
...
#                          #
############################
```

### Paso 3: Agregar la casa de fantasmas
```
######## ##########
######## ##########
########          ##########
######## ###--### ##########
######## #GGGGGG# ##########
         #GGGGGG#   
######## #GGGGGG# ##########
######## ######## ##########
```

**Importante:** 
- 6 posiciones `G` (para 4 fantasmas + espacio)
- Puerta `-` horizontal en la parte superior
- Simetría recomendada

### Paso 4: Diseñar pasillos y paredes
```
Tips:
- Mantén simetría (no obligatorio pero visualmente agradable)
- Crea rutas de escape
- Evita callejones sin salida largos
- Deja espacio para estrategia
```

### Paso 5: Colocar puntos
```
- Puntos pequeños (.) en todos los pasillos
- 4 Power Pellets (o) en esquinas estratégicas
- Dejar espacios vacíos en:
  * Casa de fantasmas
  * Alrededor de posición inicial de Pac-Man
  * Túneles laterales (opcional)
```

### Paso 6: Colocar a Pac-Man
```
- Un solo carácter P
- En zona segura (lejos de fantasmas)
- Generalmente en parte inferior
- Con espacio de maniobra
```

### Ejemplo de Área Inicial de Pac-Man:
```
#......##....##....##......#
#.##########.##.##########.#
#..........P.##............#  ← Pac-Man aquí
############################
```

---

## ✅ Validación de Mapas

### Checklist Antes de Usar un Mapa:

**Dimensiones:**
- [ ] Exactamente 28 columnas
- [ ] Exactamente 31 filas
- [ ] Todas las filas tienen la misma longitud

**Elementos Obligatorios:**
- [ ] 1 Pac-Man (`P`)
- [ ] 4-6 Fantasmas (`G`)
- [ ] 1 Puerta (`-`)
- [ ] 4 Power Pellets (`o`) mínimo
- [ ] ~200+ Puntos pequeños (`.`)

**Bordes:**
- [ ] Fila superior completamente con `#`
- [ ] Fila inferior completamente con `#`
- [ ] Columna izquierda completamente con `#`
- [ ] Columna derecha completamente con `#`

**Jugabilidad:**
- [ ] Todos los pasillos están conectados
- [ ] No hay áreas aisladas sin acceso
- [ ] Casa de fantasmas accesible solo por puerta
- [ ] Pac-Man puede llegar a todos los puntos

**Casa de Fantasmas:**
- [ ] Centro del mapa (aproximadamente)
- [ ] Encerrada por paredes
- [ ] Una puerta de entrada/salida
- [ ] 6 posiciones G dentro

---

## 🎨 Plantilla Vacía para Nuevos Mapas

```
############################
#                          #
#                          #
#                          #
#                          #
#                          #
#                          #
#                          #
#                          #
#                          #
######         ##    #######
######         ##    #######
######                ######
###### ############ #######
###### #          # #######
       #          #        
###### #          # #######
###### ############ #######
######                ######
###### ############ #######
###### ############ #######
#                          #
#                          #
#                          #
#                          #
#                          #
#                          #
#                          #
#                          #
#                          #
#                          #
############################
```

---

## 💡 Tips de Diseño Avanzado

### Dificultad Progresiva:

**Nivel Fácil:**
- Pasillos anchos (3-4 celdas)
- Pocas paredes internas
- Muchas rutas de escape
- Power pellets accesibles

**Nivel Medio:**
- Pasillos medianos (2-3 celdas)
- Más paredes internas
- Algunas zonas estrechas
- Power pellets estratégicos

**Nivel Difícil:**
- Pasillos estrechos (1-2 celdas)
- Laberinto complejo
- Callejones sin salida
- Power pellets en zonas peligrosas

### Patrones Clásicos:

**Túnel Lateral (opcional):**
```
#                          #
                             ← Túnel que conecta lados
#                          #
```

**Zona Central:**
```
#............##............#  ← Amplia zona de combate
#.####.#####.##.#####.####.#
```

**Esquinas Estratégicas:**
```
#o####.#####.##.#####.####o#  ← Power pellets en esquinas
```

---

## 🚀 Carga del Mapa en C#

### Ejemplo de Código:

```csharp
public class MapLoader
{
    public static char[,] LoadMap(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        int rows = lines.Length;
        int cols = lines[0].Length;
        
        char[,] map = new char[rows, cols];
        
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                map[i, j] = j < lines[i].Length ? lines[i][j] : ' ';
            }
        }
        
        return map;
    }
}
```

---

## 📊 Cálculo de Puntaje Máximo

```
Fórmula:
Puntaje Máximo = (Puntos Pequeños × 10) + (Power Pellets × 50) + (Frutas × Bonus)

Ejemplo Level 1:
- 244 puntos pequeños × 10 = 2,440
- 4 power pellets × 50 = 200
- Total base: 2,640 puntos

+ Fantasmas comidos durante power-up
+ Frutas bonus
= Puntaje Total Posible
```

---

## 🎯 Buenas Prácticas

1. **Testea tu mapa:** Juega varios niveles para verificar balance
2. **Simetría:** Ayuda visualmente pero no es obligatoria
3. **Variedad:** Cada nivel debe sentirse diferente
4. **Progresión:** Aumenta dificultad gradualmente
5. **Documenta:** Añade comentarios sobre características especiales

---

## 📝 Notas Finales

- Los mapas se cargan desde `/Assets/Maps/`
- Formato de nombre: `level{número}.txt`
- Puedes crear infinitos niveles
- El juego puede rotar entre mapas disponibles
- Considera crear un mapa "tutorial" simple

---

## 🔗 Referencias

- Pac-Man Original: 28×31 celdas
- Casa de Fantasmas: Siempre central
- Power Pellets: Tradicionalmente 4 en esquinas
- Diseño Clásico: Simétrico verticalmente

---

**Autor:** Diego Alejandro  
**Proyecto:** Pac-Man Educational Recreation  
**Fecha:** 2026  
**Licencia:** MIT
