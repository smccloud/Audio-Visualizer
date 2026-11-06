// Jenkins CI for the .NET 10 AudioVisualizer solution on a Windows agent.
//
// Prerequisites
//   * A .NET 10 SDK on PATH (the pipeline prints `dotnet --info` to confirm).
//     Alternatively configure a .NET SDK tool in Jenkins and uncomment the
//     tools block below.
//   * The app targets net10.0-windows. This pipeline runs on Windows.
//   * The Jenkins MSTest plugin is required to publish the TRX test results.
//
// Agent: runs on a Windows agent labeled 'Windows-Agent'.
//
// The Windows installer (installer/AudioVisualizer.Installer.wixproj) is built
// from a self-contained win-x64 publish. This pipeline builds the MSI.
//

pipeline {
    agent { label 'Windows-Agent' }
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
        // Full solution: this pipeline runs on Windows, so the WiX installer
        // project can be built here as well.
        SOLUTION                   = 'AudioVisualizer.slnx'
    }

    stages {
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

        stage('Test') {
            steps {
                script {
                    def testProjects = findFiles(glob: '**/*Tests.csproj').findAll {
                        !it.path.contains('/bin/') && !it.path.contains('/obj/')
                    }

                    if (testProjects) {
                        bat 'if exist TestResults rmdir /s /q TestResults'
                        // Single quotes are not stripped by cmd.exe, so the
                        // logger must be passed without quoting: a quoted value
                        // reaches dotnet as "'trx" and fails to load.
                        bat "dotnet test ${SOLUTION} --configuration ${CONFIGURATION} --no-build --logger trx --results-directory TestResults"
                        currentBuild.displayName = "${currentBuild.displayName} [Tests Run]"
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
                        if (env.TESTS_RUN == 'true' &&
                            findFiles(glob: 'TestResults/**/*.trx').length > 0) {
                            mstest testResultsFile: 'TestResults/**/*.trx', keepLongStdio: true
                        }
                    }
                }
            }
        }

        stage('Publish (win-x64)') {
            steps {
                // Match the shape the installer consumes: self-contained,
                // single-file, so no .NET runtime is required on the target.
                bat 'if exist publish rmdir /s /q publish'
                bat "dotnet publish AudioVisualizer/AudioVisualizer.csproj --configuration ${CONFIGURATION} --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false --output publish"
                bat 'dir publish'
            }
        }
    }

    post {
        success {
            archiveArtifacts artifacts: 'publish/**', fingerprint: true
        }
        always {
            echo "Build ${env.BUILD_NUMBER} finished: ${currentBuild.currentResult}"
        }
    }
}
