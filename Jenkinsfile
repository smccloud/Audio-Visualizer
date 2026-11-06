// Jenkins CI for the .NET 10 AudioVisualizer solution.
//
// The pipeline adapts to the agent it lands on:
//   * Windows nodes run the full build, including the WiX MSI installer
//     (installer/AudioVisualizer.Installer.wixproj) and a self-contained,
//     single-file win-x64 publish.
//   * Linux/non-Windows nodes use AudioVisualizer.Linux.slnf, which excludes
//     the installer because WiX cannot produce an MSI on Linux, and publish a
//     framework-dependent win-x64 build.
//
// Prerequisites
//   * A .NET 10 SDK on PATH (the pipeline prints `dotnet --info` to confirm).
//   * The Jenkins MSTest plugin is required to publish the TRX test results.
//
// Agent: `any`; the platform is detected at runtime by the 'Detect platform'
// stage. To pin the pipeline, replace `agent any` with a label.

pipeline {
    agent any
    triggers {
        githubPush()
    }
    // tools {
    //     dotnet 'dotnet-10'
    // }

    options {
        buildDiscarder(logRotator(numToKeepStr: '15', artifactNumToKeepStr: '15'))
        disableConcurrentBuilds()
        timeout(time: 30, unit: 'MINUTES')
    }

    environment {
        CONFIGURATION              = 'Release'
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        DOTNET_NOLOGO              = '1'
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
        // Keep SDK/NuGet state inside the workspace so builds do not depend on
        // the Jenkins user's home directory.
        DOTNET_CLI_HOME            = "${WORKSPACE}/.dotnet"
        // Overridden by the 'Detect platform' stage.
        SOLUTION                   = 'AudioVisualizer.Linux.slnf'
    }

    stages {
        stage('Detect platform') {
            steps {
                script {
                    def windows = !isUnix()
                    env.IS_WINDOWS = windows ? 'true' : 'false'
                    // Windows builds the WiX installer; Linux uses the filter
                    // that excludes it.
                    env.SOLUTION = windows ? 'AudioVisualizer.slnx' : 'AudioVisualizer.Linux.slnf'
                    currentBuild.displayName = "${currentBuild.displayName} [${windows ? 'Windows' : 'All'}]"
                    echo "Running on ${env.NODE_NAME} (${windows ? 'Windows' : 'non-Windows'}); solution: ${env.SOLUTION}"
                }
            }
        }

        stage('Verify SDK') {
            steps {
                script {
                    if (env.IS_WINDOWS == 'true') {
                        bat 'dotnet --version'
                        bat 'dotnet --info'
                    } else {
                        sh 'dotnet --version'
                        sh 'dotnet --info'
                    }
                }
            }
        }

        stage('Restore') {
            steps {
                script {
                    if (env.IS_WINDOWS == 'true') {
                        bat "dotnet restore ${SOLUTION}"
                    } else {
                        sh "dotnet restore ${SOLUTION}"
                    }
                }
            }
        }

        stage('Build') {
            steps {
                script {
                    if (env.IS_WINDOWS == 'true') {
                        bat "dotnet build ${SOLUTION} --configuration ${CONFIGURATION} --no-restore --warnaserror"
                    } else {
                        sh "dotnet build ${SOLUTION} --configuration ${CONFIGURATION} --no-restore --warnaserror"
                    }
                }
            }
        }

        stage('Test') {
            steps {
                script {
                    // No Pipeline Utility Steps plugin here, so probe the known
                    // test project directly instead of globbing.
                    if (fileExists('AudioVisualizer.Tests/AudioVisualizer.Tests.csproj')) {
                        def removeResults = env.IS_WINDOWS == 'true'
                            ? 'if exist TestResults rmdir /s /q TestResults'
                            : 'rm -rf TestResults'
                        if (env.IS_WINDOWS == 'true') {
                            bat removeResults
                            // Single quotes are not stripped by cmd.exe, so the
                            // logger must be passed without quoting.
                            bat "dotnet test ${SOLUTION} --configuration ${CONFIGURATION} --no-build --logger trx --results-directory TestResults"
                        } else {
                            sh removeResults
                            sh "dotnet test ${SOLUTION} --configuration ${CONFIGURATION} --no-build --logger trx --results-directory TestResults"
                        }
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
                        def trxFound = false
                        if (env.IS_WINDOWS == 'true') {
                            trxFound = bat(
                                script: 'dir /b /s TestResults\\*.trx >nul 2>&1',
                                returnStatus: true
                            ) == 0
                        } else {
                            trxFound = sh(
                                script: 'ls TestResults/*.trx >/dev/null 2>&1',
                                returnStatus: true
                            ) == 0
                        }

                        if (env.TESTS_RUN == 'true' && trxFound) {
                            mstest testResultsFile: 'TestResults/*.trx', keepLongStdio: true
                        }
                    }
                }
            }
        }

        stage('Publish (win-x64)') {
            steps {
                script {
                    if (env.IS_WINDOWS == 'true') {
                        // Match the shape the installer consumes: self-contained,
                        // single-file, so no .NET runtime is needed on the target.
                        bat 'if exist publish rmdir /s /q publish'
                        bat "dotnet publish AudioVisualizer/AudioVisualizer.csproj --configuration ${CONFIGURATION} --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false --output publish"
                        bat 'dir publish'
                    } else {
                        sh 'rm -rf publish'
                        sh "dotnet publish AudioVisualizer/AudioVisualizer.csproj --configuration ${CONFIGURATION} --runtime win-x64 --self-contained false --output publish"
                        sh 'ls -la publish'
                    }
                }
            }
        }
    }

    post {
        success {
            script {
                if (env.IS_WINDOWS == 'true') {
                    archiveArtifacts artifacts: 'publish/**, installer/bin/**/*.msi', fingerprint: true
                } else {
                    archiveArtifacts artifacts: 'publish/**', fingerprint: true
                }
            }
        }
        always {
            echo "Build ${env.BUILD_NUMBER} finished: ${currentBuild.currentResult}"
        }
    }
}
