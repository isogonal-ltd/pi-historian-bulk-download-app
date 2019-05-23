# PI Historian Bulk Download Application #

[Isogonal](https://isogonal.co.nz)

contact@isogonal.co.nz

This is a community project, and not provided or supported by OSISoft in any way. It was developed to download data for an internal project and is open-sourced in the hope it will be useful to someone else. Please get in touch at the email above if you have any questions or would like some assisstance.

## Overview

If you want to download 100,000+ tags and 50+ GB of data from PI Historian over a VPN connection, in a single operation, this application will support it. It can cope with recorded values, interpolated values, is fairly fault-tolerant, and has a GUI.

Four use cases:

* List all tags in PI Archive
* List all attributes in PI AF
* Retrieve recorded values for an arbitrary number of tags for an arbitrary time range per tag
* Retrieve interpolated values for an arbitrary number of tags for an arbitrary time range per tag

Features:

* Utilizes a configurable number of threads and makes one call per tag per page
* Utilizes a configurable page size, and handles joining pages together internally
* No size limit for individual tags
* Writes data for each tag to a separate file
* Alternatively, split output files into subfolders by year, month, or day
* Automatically handles exceptions and retries failed calls
* One tag throwing an exception (e.g. due to a corrupt archive) does not affect download of other tags
* All actions log information to a configurable file, as well as to the console or the GUI
* When given a User Interrupt exists cleanly at the conclusion of the tags currently in progress

Further Features:

* Asking for interpolated data from time ranges with no recorded values is really slow on the PI Server, so if interpolated values are requested, the application conducts an initial query to retrieve the first few recorded values for the tag after the requested start date, and starts the interpolated query from the first data date onwards, not the date requested in the input file.
* Does not use a single bulk RPC call, as it is harder to gracefully recover from timeout and corrupted archive exceptions using this approach.

Notes:

* Only downloads data for an attribute if the attribute is backed by a PIPoint.
* Correctly handles all of the issues described in [this pisquare post](https://pisquare.osisoft.com/thread/40099-deep-dive-explaining-custom-getlargerecordedvalues-as-a-workaround-to-arcmaxcollect) except the pathological case where there are more points at the exact same timestamp than the page size, which is highly unlikely.

## Command line interface

#### List tags in PI

```
.\Release\ListTags\ListTags.exe ^
  -o ".\Examples\ListTags\Tags.csv" ^
  -l ".\Examples\ListTags\Tags.log" ^
  -a "<PI Data Archive server>"
```

#### List attributes in PI AF

```
.\Release\ListAttributes\ListAttributes.exe ^
  -o ".\Examples\ListAttributes\Attributes.csv" ^
  -l ".\Examples\ListAttributes\Attributes.log" ^
  -a "<PI Data Archive server>" ^
  -b "<PI Asset Framework server>" ^
  -d "<PI Asset Framework database name>"
```

#### Retrieve data from PI

Requesting data from PI is based around a query file, used as an input to the program. The query file lists one tag or AF attribute per line, with a start date, an end date, and an optional interpolation period. This file could look like:

```
AAA.AAAA.AAA_AA_1.AA,1998-12-29T00:00:00,2019-05-15T00:00:00,600
AAA.AAAA.AAA_AA_2.AA,1998-12-29T00:00:00,2019-05-15T00:00:00,600
AAA.AAAA.AAA_AA_3.AA,1998-12-29T00:00:00,2019-05-15T00:00:00,600
```

Where the columns are `<tag string>,<date start>,<date end>,<interpolation interval in seconds>`. 

There are four types of input file - recorded values or interpolated values for either Tags or Asset Framework Attributes. The columns in each input file are below (to come when I transfer this into a Markdown table).

The command line interface for each of the four query types is shown below in Table 6. The command-line argument descriptions are given below (to come when I transfer this into a Markdown table). But generally speaking there is an input file containing the requested tags and time ranges, an output directory, a log file, the PI server and database details, the number of parallel threads, whether to bin output files into year/month/day folders, the number of contiguous years to bin together, and the page size. 

```
RetrieveTagData.exe ^
  -i "RecordedTag" ^
  -f ".\Examples\RecordedTag\CircuitBreakerPositionRecordedTagQuery.csv" ^
  -o ".\Examples\RecordedTag\CircuitBreakerPositionRecordedTagQuery" ^
  -l ".\Examples\RecordedTag\CircuitBreakerPositionRecordedTagQuery.log" ^
  -a "<PI Data Archive server>" ^
  -b "<PI Asset Framework server>" ^
  -d "<PI Asset Framework database name>" ^
  -p "4" ^
  -t "none" ^
  -y "5" ^
  -s "200000"
```

```
RetrieveTagData.exe ^
  -i "InterpolatedTag" ^
  -f ".\Examples\InterpolatedTag\CircuitBreakerCurrentInterpolatedTagQuery.csv" ^
  -o ".\Examples\InterpolatedTag\CircuitBreakerCurrentInterpolatedTagQuery" ^
  -l ".\Examples\InterpolatedTag\CircuitBreakerCurrentInterpolatedTagQuery.log" ^
  -a "<PI Data Archive server>" ^
  -b "<PI Asset Framework server>" ^
  -d "<PI Asset Framework database name>" ^
  -p "4" ^
  -t "none" ^
  -y "5" ^
  -s "5000"
```

```
RetrieveTagData.exe ^
  -i "RecordedAttribute" ^
  -f ".\Examples\RecordedAttribute\CircuitBreakerPositionRecordedAttributeQuery.csv" ^
  -o ".\Examples\RecordedAttribute\CircuitBreakerPositionRecordedAttributeQuery" ^
  -l ".\Examples\RecordedAttribute\CircuitBreakerPositionRecordedAttributeQuery.log" ^
  -a "<PI Data Archive server>" ^
  -b "<PI Asset Framework server>" ^
  -d "<PI Asset Framework database name>" ^
  -p "4" ^
  -t "none" ^
  -y "5" ^
  -s "200000"
```  

```
RetrieveTagData.exe ^
  -i "InterpolatedAttribute" ^
  -f ".\Examples\InterpolatedAttribute\CircuitBreakerCurrentInterpolatedAttributeQuery.csv" ^
  -o ".\Examples\InterpolatedAttribute\CircuitBreakerCurrentInterpolatedAttributeQuery" ^
  -l ".\Examples\InterpolatedAttribute\CircuitBreakerCurrentInterpolatedAttributeQuery.log" ^
  -a "<PI Data Archive server>" ^
  -b "<PI Asset Framework server>" ^
  -d "<PI Asset Framework database name>" ^
  -p "4" ^
  -t "none" ^
  -y "5" ^
  -s "5000"
```

## GUI

To come once proprietary information stripped from screenshots
