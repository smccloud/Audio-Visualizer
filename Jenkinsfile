// Jenkins CI for the .NET 10 AudioVisualizer solution on Windows.
//
// Agent: pinned to 'Windows-Agent'. The built-in (controller) node is never
// used, even when it is the only node online; if no Windows agent is available
// the build waits in the queue.
//
// Prerequisites
//   * A .NET 10 SDK on PATH (the pipeline prints `dotnet --info` to confirm).
//   * WSL with a default Linux distribution for the Ultimate Bug Scanner stage
//     (the scanner is installed into the WSL user's ~/.local/bin on first use).
//   * The Jenkins MSTest plugin is required to publish the TRX test results.
//
// The Windows installer (installer/AudioVisualizer.Installer.wixproj) is built
// from a self-contained win-x64 publish. This pipeline builds the MSI.

pipeline {
    // Never schedule on the built-in node: only the Windows agent is eligible.
    agent { label 'Windows-Agent' }
    triggers {
        githubPush()
    }
    // tools {
    //     dotnet 'dotnet-10'
    // }

    options {
        buildDiscarder(logRotator(numToKeepStr: '10', artifactNumToKeepStr: '10'))
        disableConcurrentBuilds()
        timeout(time: 30, unit: 'MINUTES')
        // Take control of checkout so the workspace can be wiped first.
        skipDefaultCheckout(true)
    }

    environment {
        CONFIGURATION              = 'Release'
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        DOTNET_NOLOGO              = '1'
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
        // Keep SDK/NuGet state inside the workspace so builds do not depend on
        // the Jenkins user's home directory.
        DOTNET_CLI_HOME            = "${WORKSPACE}/.dotnet"
        SOLUTION                   = 'AudioVisualizer.slnx'
        INSTALLER_PROJECT          = 'installer/AudioVisualizer.Installer.wixproj'
        MSI_PATH                   = 'installer\\bin\\x64\\Release\\AudioVisualizer.msi'
    }

    stages {
        stage('Clean workspace') {
            steps {
                // Wipe the entire workspace (bin/obj/publish/MSI output and any
                // other leftovers) before building. Deferred wipeout is
                // disabled so the files are really gone before the build
                // starts instead of at the end of the run. deleteDirs must be
                // true: leaving directories behind keeps an emptied .git
                // directory, which the git plugin then reports as corrupt.
                cleanWs(deleteDirs: true, disableDeferredWipeout: true)
                deleteDir()
            }
        }

        stage('Checkout') {
            steps {
                checkout scm
            }
        }
		
		stage('Ultimate Bug Scanner (WSL)') {
            steps {
                script {
                    echo 'Running UBS against source'
                    // UBS_ALLOW_PARTIAL is a shell variable assignment and the
                    // trailing glob must be expanded inside WSL, so the command
                    // has to run through bash rather than `wsl <command>`.
                    // UBS still exits non-zero when it reports findings, so
                    // treat the scan as advisory and keep the SARIF report.
                    def ubsStatus = bat(
                        script: 'wsl bash -lc "UBS_ALLOW_PARTIAL=1 /home/shaun/ultimate_bug_scanner/./ubs --no-auto-update --format=sarif --output=/mnt/c/Jenkins/workspace/Audio-Visualizer/ubs.sarif /mnt/c/Jenkins/workspace/Audio-Visualizer/*"',
                        returnStatus: true
                    )
                    echo "UBS finished with exit code ${ubsStatus} (0 = clean, 1 = findings)."
                    archiveArtifacts artifacts: 'ubs.sarif', allowEmptyArchive: true
                }
            }
        }

        stage('Verify SDK') {
            steps {
                bat 'dotnet --version'
                bat 'dotnet --info'
            }
        }

        stage('Restore') {
            steps {
                bat "dotnet restore ${SOLUTION}"
            }
        }

        stage('Build') {
            steps {
                bat "dotnet build ${SOLUTION} --configuration ${CONFIGURATION} --no-restore --warnaserror"
            }
        }

        stage('Build Installer') {
            steps {
                // The solution build normally produces the MSI. Build the WiX
                // project explicitly and verify the output so a skipped or
                // silently failed installer can never pass the pipeline.
                // -p:Platform=x64 keeps the output at bin\x64\Release.
                bat "dotnet build ${INSTALLER_PROJECT} --configuration ${CONFIGURATION} --no-restore --warnaserror -p:Platform=x64"
                bat "if not exist ${MSI_PATH} exit /b 1"
                bat "dir ${MSI_PATH}"
                // Publish a stable, easy-to-download copy as a build artifact.
                bat 'if not exist artifacts mkdir artifacts'
                bat "copy /y ${MSI_PATH} artifacts\\AudioVisualizer.msi"
            }
        }

        stage('Test') {
            steps {
                script {
                    // No Pipeline Utility Steps plugin here, so probe the known
                    // test project directly instead of globbing.
                    if (fileExists('AudioVisualizer.Tests/AudioVisualizer.Tests.csproj')) {
                        bat 'if exist TestResults rmdir /s /q TestResults'
                        // Single quotes are not stripped by cmd.exe, so the
                        // logger must be passed without quoting: a quoted value
                        // reaches dotnet as "'trx" and fails to load.
                        bat "dotnet test ${SOLUTION} --configuration ${CONFIGURATION} --no-build --logger trx --results-directory TestResults"
                        currentBuild.displayName = "${currentBuild.displayName} [Tests]"
                        env.TESTS_RUN = 'true'
                    } else {
                        echo 'No test projects found; skipping dotnet test.'
                        env.TESTS_RUN = 'false'
                    }
                }
            }
            post {
                always {
                    script {
                        // The MSTest plugin fails the build when the TRX glob
                        // matches nothing, so only publish when files exist.
                        def trxFound = bat(
                            script: 'dir /b /s TestResults\\*.trx >nul 2>&1',
                            returnStatus: true
                        ) == 0

                        if (env.TESTS_RUN == 'true' && trxFound) {
                            mstest testResultsFile: 'TestResults/*.trx', keepLongStdio: true
                        }
                    }
                }
            }
        }

        stage('Publish (win-x64)') {
            steps {
                // Match the shape the installer consumes: self-contained,
                // single-file, so no .NET runtime is needed on the target.
                bat 'if exist publish rmdir /s /q publish'
                bat "dotnet publish AudioVisualizer/AudioVisualizer.csproj --configuration ${CONFIGURATION} --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false --output publish"
                bat 'dir publish'
            }
        }
    }

    post {
        success {
            archiveArtifacts artifacts: 'publish/**, artifacts/AudioVisualizer.msi', fingerprint: true
        }
        always {
            echo "Build ${env.BUILD_NUMBER} finished: ${currentBuild.currentResult}"
        }
    }
}
