# Build WinGuard entirely in your web browser

You do **not** need Visual Studio or the .NET SDK installed on your Windows PC.
GitHub Actions can compile WinGuard on a Windows cloud runner.

## First-time setup

1. Sign in to GitHub and create a **private repository**.
2. Extract `WinGuardAgent.zip` on your device.
3. In the new GitHub repository, choose **Add file → Upload files**.
4. Upload the **contents inside the `WinGuardAgent` folder**, including the hidden `.github` folder.
   - The repository root should contain `WinGuardAgent.sln`, `README.md`, `WinGuardAgent/`, `WinGuardAdmin/`, `Installer/`, and `.github/`.
5. Commit the uploaded files to the repository.

The workflow normally starts automatically after the files are committed to `main` or `master`.

## Build manually

1. Open the repository's **Actions** tab.
2. Select **Build WinGuard for Windows**.
3. Choose **Run workflow**.
4. Open the completed workflow run.
5. Under **Artifacts**, download **WinGuard-Installer**.
6. Extract the downloaded artifact. It contains `WinGuard-Installer.zip`.
7. Extract that ZIP and run `WinGuardAgentSetup.msi` on the Windows 11 PC.
8. Approve the Windows administrator prompt.

## What GitHub builds

The cloud build publishes self-contained Windows x64 binaries and packages them with the WiX installer:

- `WinGuardAgentSetup.msi` — installer
- `WinGuardAgent.exe` — Windows service / CLI
- `WinGuardAdmin.exe` — graphical parent dashboard
- `README.md`
- `INSTALL.txt`

## Security note

Keep the GitHub repository **private** if you do not want your WinGuard source code to be public. Never add passwords, API keys, or Windows credentials to the repository or workflow.
