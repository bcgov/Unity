# Instructions to Install Unity Project

## Step 1: Create templates from code

You can create the required templates using the web OpenShift console or the oc CLI.
```
oc create -f .\database\unity-database.yaml
oc create -f .\openshift\tools-networkpolicy.yaml
oc create -f .\openshift\unity-networkpolicy.yaml
oc create -f .\openshift\unity-s3-object-storage.yaml
oc create -f .\openshift\unity-grantmanager-dbmigrator-job.yaml
oc create -f .\openshift\unity-grantmanager-dbmigrator-pipeline.yaml
oc create -f .\openshift\unity-grantmanager-dbmigrator.yaml
oc create -f .\openshift\unity-grantmanager-web.yaml
oc create -f .\openshift\unity-release-pipeline.yaml
oc create -f .\openshift\unity-release-triggers.yaml
```

## Step 2: Create .env paramater files

As a best practice, store copies of these files in a secure location.
```
.\Unity\d18498-dev-web.env
.\Unity\d18489-dev.env
.\Unity\d18498-dev-dbmigrator.env
.\Unity\d18498-dev-S3.env
```
Use oc get templates to find all available parameters of a project template.
```

oc get templates
NAME                                     DESCRIPTION                                                                     PARAMETERS          OBJECTS
tools-networkpolicy                      Template for tools namespace comunications in OpenShift.                        2 (1 generated)     2
unity-database                           PostgreSQL database service, with persistent storage....                        12 (2 generated)    6
unity-grantmanager-dbmigrator            Template for building a DotNet console application on OpenShift.                10 (3 generated)    4
unity-grantmanager-dbmigrator-job        Template for running a DotNet console application once in OpenShift.            6 (1 generated)     1
unity-grantmanager-dbmigrator-pipeline   Template for running a console application build and deployment in OpenShift.   4 (1 generated)     1
unity-grantmanager-web                   Template for running a DotNet web application on OpenShift.                     22 (10 generated)   9
unity-networkpolicy                      Template for commuication rules in OpenShift.                                   4 (1 generated)     3
unity-release-pipeline                   Template for running an application build and deployment in OpenShift.          4 (1 generated)     1
unity-release-triggers                   Template for triggering an application build pipeline in OpenShift.             5 (2 generated)     5
unity-s3-object-storage                  Template for S3 connection information in OpenShift.                            8 (3 generated)     2
```

## Step 3: Create or replace project resources

You can create OpenShift resources using the web OpenShift console or the oc CLI.

Using the command line,
```
oc process unity-networkpolicy --param-file=d18498-dev.env | oc create -f -
oc process unity-s3-object-storage --param-file=d18498-dev-S3.env | oc create -f -
oc process unity-database --param-file=d18498-dev.env | oc create -f -
oc process unity-release-pipeline --param-file=d18498-dev.env | oc create -f -
oc process unity-release-triggers --param-file=d18498-dev.env | oc create -f -
oc process unity-grantmanager-dbmigrator-pipeline --param-file=d18498-dev.env | oc create -f -

oc process unity-grantmanager-dbmigrator --param-file=d18498-dev-dbmigrator.env | oc create -f -
oc process unity-grantmanager-dbmigrator-job --param-file=d18498-dev.env | oc create -f -
oc process unity-grantmanager-web --param-file=d18498-dev-web.env | oc create -f -
```
## Step 4: Delete all project resources

```oc delete all,eventlisteners,triggerbinding,triggertemplates,pipelines --selector app.kubernetes.io/part-of=Triggers 
oc delete all,pipelines,pipelineruns --selector app.kubernetes.io/part-of=unity-grantmanager
oc delete secrets,configmaps --selector app.kubernetes.io/part-of=Triggers
oc delete secrets,configmaps --selector app.kubernetes.io/part-of=unity-grantmanager 
oc delete templates,networkpolicies --all
```
