# Kart Physics Simulator for Unity

A realistic go-kart physics simulator for Unity featuring detailed engine modeling, tire physics, and telemetry visualization. This system provides accurate vehicle dynamics with configurable parameters for fine-tuning kart behavior.

## ✨ Features

### 🏎️ Advanced Physics Simulation
- **Realistic engine model** with torque curve, inertia, rev limiter, and throttle response
- **Proper drivetrain simulation** with gear ratio and efficiency calculations
- **Weight distribution** between front and rear axles
- **Friction circle model** for combined longitudinal/lateral tire forces
- **Rolling resistance** and engine friction losses

### 🎮 Input System
- Built-in Unity Input System support
- Analog throttle and steering controls
- Handbrake for controlled drifting
- Smooth input filtering

### 📊 Comprehensive Telemetry
- Real-time RPM, torque, and speed monitoring
- Wheel-specific force visualization (Fx, Fy)
- Slip ratio and slip angle displays
- Engine parameter breakdown (drive torque, losses, net torque)
- On-screen GUI with multi-column layout

### 🔧 Configurable Parameters
- **KartSettings ScriptableObject** for easy tuning
- Adjustable mass, friction coefficients, and stiffness
- Customizable engine torque curve
- Configurable weight distribution
- Tunable handbrake behavior for drift control

## 🛠️ Technical Implementation

### Core Components

1. **KartSettings** (ScriptableObject)
   - Centralized configuration asset
   - Physics, engine, and drivetrain parameters
   - Easy to create and share configurations

2. **KartEngine**
   - Detailed internal combustion engine simulation
   - Torque curve-based power delivery
   - Inertia and response modeling
   - Rev limiter with smooth cutoff
   - Engine friction and load calculations

3. **KartController**
   - Main physics controller with Rigidbody integration
   - Per-wheel force calculations
   - Tire model with lateral stiffness
   - Handbrake-induced drift mechanics
   - Telemetry data collection and display

### Physics Model
- **Longitudinal forces**: Engine torque, rolling resistance, handbrake
- **Lateral forces**: Tire cornering stiffness based on slip velocity
- **Normal forces**: Static weight distribution
- **Force limiting**: Friction circle constraint (μN limit)
- **Wheel kinematics**: Proper steering geometry

## 🚀 Getting Started

### Installation
1. Import the scripts into your Unity project
2. Create a KartSettings asset via `Create > Karting > Kart Settings`
3. Configure your kart parameters
4. Attach KartController and KartEngine components to your kart GameObject
5. Assign wheel transforms and input actions
6. Configure the Rigidbody component

### Input Setup
1. Create Input Actions for steering and throttle (Vector2) and handbrake (Button)
2. Assign these to the KartController component
3. Recommended: Use gamepad for analog control

### Basic Configuration
```csharp
// Example KartSettings values:
- Mass: 80 kg
- Friction Coefficient: 4.0
- Front/Rear Lateral Stiffness: 1000 N/m
- Max Steer Angle: 30 degrees
- Engine Max RPM: 8000
- Gear Ratio: 8.0
- Wheel Radius: 0.3 m
```

## 📈 Telemetry

The system provides comprehensive real-time data:
- **Speed** (m/s and km/h)
- **Engine RPM** and torque
- **Throttle** and steering inputs
- **Wheel forces** (longitudinal and lateral)
- **Slip ratios** per wheel
- **Slip angle** during cornering
- **Engine breakdown** (drive, friction, load torques)

Toggle telemetry display via the `_showTelemetry` checkbox.

## 🎯 Tuning Tips

### For More Grip
- Increase friction coefficient
- Increase lateral stiffness values
- Adjust weight distribution toward rear for acceleration

### For Drift Behavior
- Use handbrake with steering input
- Reduce rear lateral stiffness
- Increase handbrake force and lateral multiplier

### Engine Response
- Modify throttle response rate
- Adjust engine inertia for faster/slower rev changes
- Tune torque curve for desired power band

## 🔍 Key Variables

### KartSettings
- `engineTorqueCurve` - Power delivery across RPM range
- `frontAxleShare` - Weight distribution (0-1)
- `frictionCoefficient` - Tire grip level
- `maxSteerAngle` - Maximum steering angle

### KartEngine
- `_throttleResponse` - Throttle input smoothing
- `_revLimiterRpm` - RPM where limiter begins
- `_engineFrictionCoeff` - Internal engine losses

### KartController
- `_handbrakeForce` - Drift initiation strength
- `_handbrakeLateralMultiplier` - Lateral force reduction during drift
- `_drivetrainEfficiency` - Power transmission loss

## 🎮 Controls
- **Steering**: Horizontal axis input
- **Throttle/Brake**: Vertical axis input
- **Handbrake**: Assigned button (default: space/shift)

## 📝 Notes
- Designed for Unity's FixedUpdate physics cycle
- Uses ForceMode.Force for realistic acceleration
- Wheel transforms should be positioned correctly
- Telemetry GUI can be customized via GUIStyle
- All calculations in metric units (N, m/s, kg)

## 🔮 Future Enhancements
- Suspension system integration
- Aerodynamic forces (downforce, drag)
- Transmission model with multiple gears
- Track surface variation
- Advanced tire model (Pacejka)
- Multiplayer synchronization
- Data logging and replay

## 📄 License
This system is provided as-is for educational and development purposes. Modify and extend as needed for your project.

---

# Физический симулятор картинга для Unity

Реалистичный симулятор физики картинга для Unity с детальным моделированием двигателя, физикой шин и визуализацией телеметрии. Система обеспечивает точную динамику транспортного средства с настраиваемыми параметрами для тонкой настройки поведения карта.

## ✨ Особенности

### 🏎️ Продвинутая симуляция физики
- **Реалистичная модель двигателя** с кривой крутящего момента, инерцией, ограничителем оборотов и откликом дросселя
- **Корректная симуляция трансмиссии** с передаточным числом и учетом КПД
- **Распределение веса** между передней и задней осями
- **Модель фрикционного круга** для комбинированных продольных/боковых сил шин
- **Сопротивление качению** и потери на трение в двигателе

### 🎮 Система ввода
- Поддержка встроенной системы ввода Unity
- Аналоговое управление газом и рулем
- Ручной тормоз для контролируемого заноса
- Сглаживание входных сигналов

### 📊 Комплексная телеметрия
- Мониторинг оборотов, крутящего момента и скорости в реальном времени
- Визуализация сил для каждого колеса (Fx, Fy)
- Отображение коэффициента проскальзывания и угла увода
- Детализация параметров двигателя (крутящий момент, потери, результирующий момент)
- Интерфейс на экране с многоколоночным расположением

### 🔧 Настраиваемые параметры
- **ScriptableObject KartSettings** для легкой настройки
- Регулируемая масса, коэффициенты трения и жесткости
- Настраиваемая кривая крутящего момента двигателя
- Конфигурируемое распределение веса
- Настраиваемое поведение ручного тормоза для контроля заноса

## 🛠️ Техническая реализация

### Основные компоненты

1. **KartSettings** (ScriptableObject)
   - Централизованный актив конфигурации
   - Параметры физики, двигателя и трансмиссии
   - Легко создавать и делиться конфигурациями

2. **KartEngine**
   - Детальная симуляция двигателя внутреннего сгорания
   - Отдача мощности на основе кривой крутящего момента
   - Моделирование инерции и отклика
   - Ограничитель оборотов с плавным срезом
   - Расчет трения и нагрузки двигателя

3. **KartController**
   - Основной контроллер физики с интеграцией Rigidbody
   - Расчет сил для каждого колеса
   - Модель шин с боковой жесткостью
   - Механика заноса при использовании ручного тормоза
   - Сбор и отображение данных телеметрии

### Модель физики
- **Продольные силы**: Крутящий момент двигателя, сопротивление качению, ручной тормоз
- **Боковые силы**: Жесткость шин при повороте на основе скорости скольжения
- **Нормальные силы**: Статическое распределение веса
- **Ограничение сил**: Ограничение фрикционного круга (предел μN)
- **Кинематика колес**: Правильная геометрия рулевого управления

## 🚀 Начало работы

### Установка
1. Импортируйте скрипты в свой проект Unity
2. Создайте актив KartSettings через `Create > Karting > Kart Settings`
3. Настройте параметры вашего карта
4. Прикрепите компоненты KartController и KartEngine к вашему GameObject карта
5. Назначьте трансформы колес и действия ввода
6. Настройте компонент Rigidbody

### Настройка ввода
1. Создайте Input Actions для руления и газа (Vector2) и ручного тормоза (Button)
2. Назначьте их компоненту KartController
3. Рекомендуется: используйте геймпад для аналогового управления

### Базовая конфигурация
```csharp
// Пример значений KartSettings:
- Масса: 80 кг
- Коэффициент трения: 4.0
- Боковая жесткость перед/зад: 1000 Н/м
- Максимальный угол поворота: 30 градусов
- Макс. обороты двигателя: 8000
- Передаточное число: 8.0
- Радиус колеса: 0.3 м
```

## 📈 Телеметрия

Система предоставляет комплексные данные в реальном времени:
- **Скорость** (м/с и км/ч)
- **Обороты двигателя** и крутящий момент
- **Положение дросселя** и рулевого управления
- **Силы на колесах** (продольные и боковые)
- **Коэффициенты проскальзывания** для каждого колеса
- **Угол увода** при поворотах
- **Детализация работы двигателя** (движущий момент, потери, нагрузка)

Переключение отображения телеметрии через чекбокс `_showTelemetry`.

## 🎯 Советы по настройке

### Для большего сцепления
- Увеличьте коэффициент трения
- Увеличьте значения боковой жесткости
- Сместите распределение веса назад для лучшего ускорения

### Для поведения в заносе
- Используйте ручной тормоз с поворотом руля
- Уменьшите боковую жесткость задних колес
- Увеличьте силу ручного тормоза и множитель боковой силы

### Отклик двигателя
- Измените скорость отклика дросселя
- Настройте инерцию двигателя для более быстрого/медленного изменения оборотов
- Настройте кривую крутящего момента для желаемой полки мощности

## 🔍 Ключевые переменные

### KartSettings
- `engineTorqueCurve` - Отдача мощности в диапазоне оборотов
- `frontAxleShare` - Распределение веса (0-1)
- `frictionCoefficient` - Уровень сцепления шин
- `maxSteerAngle` - Максимальный угол поворота руля

### KartEngine
- `_throttleResponse` - Сглаживание ввода дросселя
- `_revLimiterRpm` - Обороты, при которых включается ограничитель
- `_engineFrictionCoeff` - Внутренние потери двигателя

### KartController
- `_handbrakeForce` - Сила инициирования заноса
- `_handbrakeLateralMultiplier` - Уменьшение боковой силы во время заноса
- `_drivetrainEfficiency` - Потери при передаче мощности

## 🎮 Управление
- **Руль**: Горизонтальная ось ввода
- **Газ/Тормоз**: Вертикальная ось ввода
- **Ручной тормоз**: Назначенная кнопка (по умолчанию: пробел/shift)

## 📝 Примечания
- Разработано для цикла физики FixedUpdate в Unity
- Использует ForceMode.Force для реалистичного ускорения
- Трансформы колес должны быть правильно позиционированы
- GUI телеметрии можно настроить через GUIStyle
- Все расчеты в метрических единицах (Н, м/с, кг)

## 🔮 Планы по развитию
- Интеграция системы подвески
- Аэродинамические силы (прижимная сила, сопротивление)
- Модель трансмиссии с несколькими передачами
- Вариации поверхности трассы
- Продвинутая модель шин (Pacejka)
- Синхронизация для мультиплеера
- Логирование данных и повтор

## 📄 Лицензия
Система предоставляется "как есть" для образовательных и разработческих целей. Модифицируйте и расширяйте по мере необходимости для вашего проекта.
