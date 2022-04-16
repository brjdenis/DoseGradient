# DoseGradient
A standalone app for creating longitudinal dose gradients in the RT dose matrix.

![image](image.png)

## Setup
The program is running on .NET Framework version 4.7.2. You can either compile the program or download the executable:  [link](https://github.com/brjdenis/DoseGradient/releases/download/v1.0/DoseGradient_v1.0.zip)

In order to compile the program open the solution in Visual Studio 2019/2022. Restore NuGet packages (OxyPlot and EvilDicom) and compile for Release 64bit.

## How to use
This program serves one purpose only: to modify an existing RT dose matrix by introducing dose gradients in the longitudinal direction (z-axis). 

Export the RT plan, plan dose (not beam dose), structure set and CT images. It is best to copy the RT plan and export the copy, not the original. Make sure you export the dose with absolute values. Load the data into the program. The program only reads the dose matrix and CT images. You must load the dose matrix, but CT images are optional.

The program will check if the RT dose file is of type "PLAN" and if the CT images belong to the same series/study. If not, you will get an error.

Define the dose profile you would like to see in the longitudinal direction. Use the input textboxes. The z-coordinate is the same as in the TPS (in Eclipse 'user origin' must be set to dicom or zero). Make sure that all the points P are in increasing order of z. All dose values D must be non-negative. 

To define points P you can use the mouse and keyboard. Click on the dose plot to show the tracker. The last position of the tracker is saved into memory. On the keyboard press button '1' to save P1, '2' to save P2 etc. 

Save the matrix to a dicom file. Upload the whole dataset back into the TPS. Some systems will not allow the import if the dataset already exists. In this case delete the copy of the original RT plan from the system.

The program will change the SOPInstanceUID of the RT dose file.


## Log



## Important note

**Before using this program see the [licence](https://github.com/brjdenis/DoseGradient/blob/master/LICENSE) and make sure you understand it. The program comes with absolutely no guarantees of any kind. It is entirely on you what you do with it and what the results are.**

```
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```


## LICENSE

Published under the MIT license. The zip package contains other (NuGet) libraries that are not under the same license. 