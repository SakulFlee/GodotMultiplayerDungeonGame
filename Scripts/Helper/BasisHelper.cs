public static class BasisHelper
{
    public static Basis DefaultState = new Basis(
        0, 0, 0,
        0, 0, 0,
        0, 0, 0
    );

    public static Basis RotateOnceAroundY = new Basis(
        1, 0, 0,
        0, 1, 0,
        0, 0, 1
    );
    public static Basis RotateTwiceAroundY = new Basis(
        -1, 0, 0,
        0, 1, 0,
        0, 0, -1
    );
    public static Basis RotateThriceAroundY = new Basis(
        0, 0, 1,
        0, 1, 0,
        -1, 0, 0
    );
    public static Basis RotateFourTimesAroundY = new Basis(
        0, 0, -1,
        0, 1, 0,
        1, 0, 0
    );
}