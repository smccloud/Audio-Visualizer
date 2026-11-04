// Jenkins CI for the .NET 10 AudioVisualizer solution on a Linux agent.
//
// Prerequisites
//   * A .NET 10 SDK on PATH (the pipeline prints `dotnet --info` to confirm).
//     Alternatively configure a .NET SDK tool in Jenkins and uncomment the
//     tools block below.
//   * The app targets net10.0-windows. Linux can restore, build and publish it
//     (EnableWindowsTargeting is set in the project), but it must be run on
//     Windows — this pipeline does not run the application.
//   * The Jenkins MSTest plugin is required to publish the TRX test results.
//
// Agent: defaults to `any`. Replace with the label of your Linux build agents,
// for example: agent { label 'linux' }.

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
    }

    stages {
        stage('Verify SDK') {
            steps {
                sh 'dotnet --version'
                sh 'dotnet --info'
            }
        }

        stage('Restore') {
            steps {
                sh 'dotnet restore AudioVisualizer.slnx'
            }
        }

        stage('Build') {
            steps {
                sh "dotnet build AudioVisualizer.slnx --configuration ${CONFIGURATION} --no-restore --warnaserror"
            }
        }

        stage('Test') {
            steps {
                script {
                    def testProject = sh(
                        script: "find . -name '*Tests.csproj' -o -name '*Test.csproj' | head -n 1",
                        returnStdout: true
                    ).trim()

                    if (testProject) {
                        sh "dotnet test AudioVisualizer.slnx --configuration ${CONFIGURATION} --no-build --logger 'trx;LogFileName=results.trx' --results-directory TestResults"
                    } else {
                        echo 'No test projects found; skipping dotnet test.'
                    }
                }
            }
            post {
                always {
                    mstest testResultsFile: 'TestResults/**/*.trx', keepLongStdio: true
                }
            }
        }

        stage('Publish (win-x64)') {
            steps {
                // Linux cannot run the Windows app, but it can produce a
                // framework-dependent Windows build for packaging.
                sh 'rm -rf publish'
                sh "dotnet publish AudioVisualizer/AudioVisualizer.csproj --configuration ${CONFIGURATION} --runtime win-x64 --self-contained false --output publish"
                sh 'ls -la publish'
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
