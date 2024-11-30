# Kolbeh VDI Solution

Kolbeh VDI is a Linux desktop application that provides seamless access to virtual desktop infrastructure (VDI). Built with GTK# and .NET, it offers a native Linux experience for connecting to and managing virtual machines.

## Features

- Secure authentication with OTP
- Virtual machine management and monitoring
- Dynamic OS icon support
- Real-time VM status updates
- User-friendly GTK-based interface
- Self-contained application deployment

## Requirements

- .NET 6.0 SDK or later
- GTK 3.0
- WebKitGTK

### Installing Dependencies

On Ubuntu/Debian:
```bash
sudo apt-get update
sudo apt-get install gtk-sharp3 libwebkit2gtk-4.0-dev
```

On Fedora:
```bash
sudo dnf install gtk-sharp3 webkit2gtk3-devel
```

## Development Setup

1. Clone the repository:
```bash
git clone https://github.com/haioco/kolbeh-linux.git
cd kolbeh-linux
```

2. Install .NET dependencies:
```bash
dotnet restore
```

3. Run the application in development mode:
```bash
dotnet run
```

## Building

### Debug Build
```bash
dotnet build
```

### Release Build
```bash
dotnet build -c Release
```

### Self-contained Release
To create a self-contained application that includes the .NET runtime:
```bash
dotnet publish -r linux-x64 --self-contained true -c Release
```

The self-contained application will be available in the `bin/Release/net6.0/linux-x64/publish` directory.

## Branch Structure

The project follows a three-branch structure:
- `main`: Latest stable release
- `Development`: Active development branch
- `Production`: Production-ready code

### Development Workflow

1. Create feature branches from `Development`:
```bash
git checkout Development
git checkout -b feature/your-feature-name
```

2. Make changes and commit using conventional commit format:
```bash
git commit -m "feat: add new feature"
git commit -m "fix: resolve bug issue"
git commit -m "docs: update documentation"
```

3. Push changes and create a pull request to `Development`

4. After testing in `Development`, create a pull request to `Production`

5. After final testing, merge `Production` into `main` for release

## Commit Message Convention

We follow the Conventional Commits specification:

- `feat:` New features
- `fix:` Bug fixes
- `docs:` Documentation changes
- `style:` Code style changes (formatting, etc.)
- `refactor:` Code refactoring
- `test:` Adding or modifying tests
- `chore:` Maintenance tasks

## Versioning

We use Semantic Versioning (SemVer):
- Major version: Breaking changes
- Minor version: New features
- Patch version: Bug fixes

Current version: 0.6.0 (Beta)

## License

[Add your license information here]

## Contributing

1. Fork the repository
2. Create your feature branch from `Development`
3. Commit your changes following the commit convention
4. Push to your fork
5. Create a Pull Request

## Support

[Add support information here]
