function skeleton(path)
%path = 'test3.png';
img = imread(path);
try
    img = rgb2gray(img);
end

skel = bwmorph(img,'thin',Inf);
E = bwmorph(skel,'endpoints');
skel_save = im2uint8(skel);
skel_save(E) = 128;
imwrite(skel_save,'thin.png');
B = bwmorph(skel,'branchpoints');
result= skel;
if(~isempty(nonzeros(B)))
    E = bwmorph(skel,'endpoints');
    [y,x] = find(E);
    B_loc = B;
    Dmask = false(size(skel));
    for k = 1:numel(x)
        D = bwdistgeodesic(skel,x(k),y(k));
        distanceToBranchPt = min(D(B_loc));
        distanceToEnd = max(D(E));
        if(distanceToBranchPt<0.1*distanceToEnd)
            Dmask(D <= distanceToBranchPt) = true;
        end
    end
    skelD = skel - Dmask;
    skelD = im2bw(skelD);
    %break
    if(size(unique(nonzeros(bwlabel(skelD))),1)>1)
        skelD(B) = true;
    end
    B = bwmorph(skelD,'branchpoints');
    while(~isempty(nonzeros(B)))
        E = bwmorph(skelD,'endpoints');
        [y,x] = find(E);
        %find longest path
        maxdis = -1;
        for k = 1:numel(x)
            D = bwdistgeodesic(skelD,x(k),y(k));
            distanceToEnd = max(D(E));
            distanceToBranchPt = min(D(B));
            if(distanceToEnd>maxdis)
                Dmask = false(size(skel));
                Dmask(D <= distanceToEnd) = true;
                Dmask(D < distanceToBranchPt) = false;
            end
        end
        if(~isempty(nonzeros(Dmask)))
            skelD = Dmask;
        end
        B = bwmorph(skelD,'branchpoints');
    end
    result = skelD;
end


L = bwlabel(result);%标记连通区域
stats = regionprops(L);
Ar = cat(1, stats.Area);
ind = find(Ar ==max(Ar));%找到最大连通区域的标号
result(find(L~=ind))=0;%将其他区域置为0
result = bwmorph(result,'clean');

E = bwmorph(result,'endpoints');
result = im2uint8(result);
result(E) = 128;
imwrite(result,'prune.png');
end