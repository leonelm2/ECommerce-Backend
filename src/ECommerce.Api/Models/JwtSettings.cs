// JwtSettings ha sido movida a ECommerce.Application.Settings.JwtSettings
// para respetar las reglas de dependencia de Clean Architecture:
// Infrastructure necesita acceder a JwtSettings, pero Infrastructure no puede
// referenciar la capa Api. Al moverla a Application, todas las capas pueden usarla.
//
// Ver: src/ECommerce.Application/Settings/JwtSettings.cs
